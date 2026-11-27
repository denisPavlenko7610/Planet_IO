using RD.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using RD.Settings;
using VContainer;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using RD.SceneFlow;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class NetworkSessionService : INetworkSessionService, IStartable, IDisposable
    {
        private const string LocalAddress = "127.0.0.1";
        private const ushort LocalPort = 7777;

        private const float ProgressInitial = 0.02f;
        private const float ProgressSceneLoading = 0.04f;
        private const float ProgressSceneLoaded = 0.94f;
        private const float ProgressTrackMax = 0.9f;

        private readonly NetworkManager _networkManager;
        private readonly ConnectionApprovalHandler _approvalHandler;
        private readonly ISceneFlow _sceneFlow;
        private readonly UgsSessionConnector _sessionConnector = new();
		private readonly Dictionary<ulong, string> _approvedPlayers = new();
        private NetworkSceneManager _networkSceneManager;
		private readonly IPlayerProfileService _playerProfileService;

        private int _progressGeneration;
        private bool _subscribed;
        private bool _shutdownRequested;
        private bool _recoveringFromDisconnect;
        private ISettingsService _settings;

        [Inject]
        public void ConfigureSettings(ISettingsService settings) => _settings = settings;

        public NetworkSessionService(
            NetworkManager networkManager,
            IPlayerProfileService playerProfileService,
            ISceneFlow sceneFlow)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _approvalHandler = new ConnectionApprovalHandler(networkManager);
			_playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
        }

        public event Action<float> LoadingProgressChanged;
        public event Action<NetworkSessionState, string> StateChanged;

        public NetworkSessionState State { get; private set; } = NetworkSessionState.Offline;
        public SessionFailure LastFailure { get; private set; }
        public NetworkSessionMode Mode { get; private set; } = NetworkSessionMode.None;
        public RoomConnectionSettings CurrentRoom { get; private set; } = RoomConnectionSettings.Default;
        public string Status { get; private set; } = "Ready to connect";
        public float LoadingProgress { get; private set; }
        public bool IsServer => _networkManager != null && _networkManager.IsServer;
        public bool IsSceneEventInProgress { get; private set; }

        public void Start()
        {
            Subscribe();
        }

        public Awaitable<bool> StartHostAsync(int maxPlayers)
        {
            int playerLimit = RoomRules.ClampMaxPlayers(maxPlayers);
            return StartSessionAsync(NetworkSessionState.StartingHost, "Creating room...",
                handler => _sessionConnector.CreatePrivateRoomAsync(playerLimit, handler));
        }

        public Awaitable<bool> StartClientAsync(string roomCode)
        {
            if (!RoomRules.TryCreateConnectionSettings(roomCode, out RoomConnectionSettings room, out string validationError))
            {
                FailStart(validationError);
                return CompletedFalse();
            }

            return StartSessionAsync(NetworkSessionState.StartingClient, $"Connecting to {room.RoomCode}...",
                handler => _sessionConnector.JoinByCodeAsync(room.RoomCode, handler), room);
        }

        public Awaitable<bool> StartQuickPlayAsync()
        {
            return StartSessionAsync(NetworkSessionState.Searching, "Searching for a match...",
                handler => _sessionConnector.QuickPlayAsync(RoomRules.QuickPlayMaxPlayers, handler));
        }

        private async Awaitable<bool> StartSessionAsync(
            NetworkSessionState startingState,
            string startingStatus,
            Func<INetworkHandler, Awaitable<ISession>> connect,
            RoomConnectionSettings? requestedRoom = null)
        {
            if (!CanStartSession())
            {
                return false;
            }

            if (requestedRoom.HasValue)
            {
                CurrentRoom = requestedRoom.Value;
            }

            Mode = startingState == NetworkSessionState.StartingHost ? NetworkSessionMode.Host : NetworkSessionMode.Client;
            SetState(startingState, startingStatus);
            _sessionConnector.RequestedBotDifficulty = _settings?.Get(GameSettingKeys.BotDifficulty) ?? 1;
			NgoSessionNetworkHandler handler = new(_networkManager, PrepareConnection);

            try
            {
                ISession session = await connect(handler);
                Mode = handler.IsHost ? NetworkSessionMode.Host : NetworkSessionMode.Client;
                CurrentRoom = new RoomConnectionSettings(session.Code ?? string.Empty, session.MaxPlayers,
                    UgsSessionConnector.ReadDifficulty(session.Properties));
                SubscribeSceneManager();

                if (!handler.IsHost)
                {
                    if (State != NetworkSessionState.InGame)
                    {
                        SetState(NetworkSessionState.Connecting, $"Joined room {CurrentRoom.RoomCode}");
                    }
                    return true;
                }

				if (!await WaitForListeningAsync("Room did not transition to Listening state"))
				{
					return false;
				}

				SetState(NetworkSessionState.StartingHost, $"Room created. Code: {CurrentRoom.RoomCode}");

				return await LoadLoadingSceneAsync();
            }
            catch (OperationCanceledException)
            {
                await AbortStartAsync("Session start was cancelled");
                return false;
            }
            catch (Exception exception)
            {
                UnityLogger.LogException(exception);
                await AbortStartAsync($"Session error: {exception.Message}");
                return false;
            }
        }

        private void PrepareConnection(bool isHost)
        {
			if (isHost)
			{
				_approvedPlayers.Clear();
				_networkManager.ConnectionApprovalCallback = ApproveRoomConnection;

				return;
			}

            _networkManager.ConnectionApprovalCallback = null;
            ConnectionApprovalHandler.RoomConnectionPayload payload = new()
            {
                PlayerId = _sessionConnector.LocalIdentity,
                Protocol = RoomRules.ProtocolVersion,
                Nickname = _playerProfileService.Nickname
            };
            _networkManager.NetworkConfig.ConnectionData = ConnectionApprovalHandler.SerializePayload(payload);
        }

        private static async Awaitable<bool> CompletedFalse()
        {
            await Awaitable.NextFrameAsync();
            return false;
        }

        public async Awaitable<bool> StartSinglePlayerAsync()
        {
            RoomConnectionSettings singlePlayerRoom = new("SOLO", 1);

            if (!CanStartSession())
            {
                return false;
            }

            CurrentRoom = singlePlayerRoom;
            Mode = NetworkSessionMode.SinglePlayer;
            UseLocalTransport();
            SetState(NetworkSessionState.StartingSinglePlayer, "Starting single player");
            _networkManager.ConnectionApprovalCallback = ConnectionApprovalHandler.ApproveSinglePlayerConnection;

            if (!_networkManager.StartHost())
            {
                FailStart("Failed to start single player");
                return false;
            }

            SubscribeSceneManager();

			if (!await WaitForListeningAsync(
				"Single player did not transition to Listening state",
				"Single player start was cancelled"))
			{
				return false;
			}

			return await LoadLoadingSceneAsync();
        }

		private async Awaitable<bool> WaitForListeningAsync(string notListeningReason, string cancelledReason = null)
		{
			if (!await WaitOneFrameAsync())
			{
				await AbortStartAsync(cancelledReason ?? notListeningReason);
				return false;
			}

			if (!_networkManager.IsListening || !_networkManager.IsServer)
			{
				await AbortStartAsync(notListeningReason);
				return false;
			}

			return true;
		}

		private async Awaitable<bool> LoadLoadingSceneAsync()
		{
			if (LoadNetworkScene(SceneNames.Loading))
			{
				return true;
			}

			await AbortStartAsync(Status);
			return false;
		}

        public async Awaitable ContinueToGameAsync()
        {
            if (!IsServer)
            {
                return;
            }

            while (IsSceneEventInProgress)
            {
                if (_shutdownRequested || !_networkManager.IsListening || !IsServer)
                {
                    return;
                }

                if (!await WaitOneFrameAsync())
                {
                    return;
                }
            }

            if (!await WaitOneFrameAsync())
            {
                return;
            }

            if (!_shutdownRequested && _networkManager.IsListening && IsServer)
            {
                LoadNetworkScene(SceneNames.Game);
            }
        }

        public async Awaitable ShutdownAndReturnToMenuAsync()
        {
            if (_shutdownRequested)
            {
                return;
            }

            _shutdownRequested = true;
            SetState(NetworkSessionState.ShuttingDown, "Shutting down network session");

            try
            {
				await StopNetworkManagerAsync();
				await _sessionConnector.LeaveAsync(Mode == NetworkSessionMode.Host);
                ResetConnectionConfiguration();
                Mode = NetworkSessionMode.None;
                CurrentRoom = RoomConnectionSettings.Default;
                SetProgress(0f);

                await _sceneFlow.ChangeSceneAsync(SceneIds.Menu);
                SetState(NetworkSessionState.Offline, "Ready to connect");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _shutdownRequested = false;
            }
        }

        public void Dispose()
        {
            _shutdownRequested = true;
            _progressGeneration++;
            _networkManager.ConnectionApprovalCallback = null;
            Unsubscribe();
        }

        private void UseLocalTransport()
        {
            if (_networkManager.NetworkConfig.NetworkTransport is UnityTransport transport)
            {
                transport.SetConnectionData(LocalAddress, LocalPort);
            }
        }

        private bool CanStartSession()
        {
            if (_networkManager.IsListening || _networkManager.ShutdownInProgress)
            {
                SetState(NetworkSessionState.Failed, "Network session is already running or shutting down");
                return false;
            }

            _shutdownRequested = false;
            SetProgress(0f);
            return true;
        }

        private void FailStart(string reason)
        {
            Mode = NetworkSessionMode.None;
            CurrentRoom = RoomConnectionSettings.Default;
            ResetConnectionConfiguration();
            IsSceneEventInProgress = false;
            SetProgress(0f);
            SetState(NetworkSessionState.Failed, reason);
        }

        private async Awaitable AbortStartAsync(string reason)
        {
            await _sessionConnector.LeaveAsync();
            await StopNetworkManagerAsync();
            FailStart(reason);
        }

        private async Awaitable StopNetworkManagerAsync()
        {
            _progressGeneration++;
            IsSceneEventInProgress = false;
            UnsubscribeSceneManager();

            if (_networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }

            while (_networkManager.ShutdownInProgress)
            {
                if (!await WaitOneFrameAsync())
                {
                    return;
                }
            }
        }

        private void ResetConnectionConfiguration()
        {
            _networkManager.ConnectionApprovalCallback = null;
            _networkManager.NetworkConfig.ConnectionData = Array.Empty<byte>();
			_approvedPlayers.Clear();
        }

        private static async Awaitable<bool> WaitOneFrameAsync()
        {
            try
            {
                await Awaitable.NextFrameAsync();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private bool LoadNetworkScene(string sceneName)
        {
            if (!_networkManager.IsServer || _networkManager.SceneManager == null)
            {
                SetState(NetworkSessionState.Failed, "NetworkSceneManager is not ready yet");
                return false;
            }

            SceneEventProgressStatus result = _networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            if (result != SceneEventProgressStatus.Started)
            {
                SetState(NetworkSessionState.Failed, $"Failed to load scene {sceneName}: {result}");
                return false;
            }

            IsSceneEventInProgress = true;
            SetProgress(ProgressInitial);
            SetState(NetworkSessionState.Loading, $"Loading scene {sceneName}");
            return true;
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback += OnClientConnected;
            _networkManager.OnClientStarted += SubscribeSceneManager;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            _networkManager.OnClientStopped += OnSessionStopped;
            _networkManager.OnServerStopped += OnSessionStopped;

            _subscribed = true;
            SubscribeSceneManager();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientStarted -= SubscribeSceneManager;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnClientStopped -= OnSessionStopped;
            _networkManager.OnServerStopped -= OnSessionStopped;

            UnsubscribeSceneManager();
            _subscribed = false;
        }

        private void SubscribeSceneManager()
        {
            NetworkSceneManager sceneManager = _networkManager.SceneManager;
            if (sceneManager == null || sceneManager == _networkSceneManager)
            {
                return;
            }

            if (_networkSceneManager != null)
            {
                _networkSceneManager.OnSceneEvent -= OnSceneEvent;
            }

            _networkSceneManager = sceneManager;
            _networkSceneManager.OnSceneEvent += OnSceneEvent;
        }

        private void UnsubscribeSceneManager()
        {
            if (_networkSceneManager == null)
            {
                return;
            }

            _networkSceneManager.OnSceneEvent -= OnSceneEvent;
            _networkSceneManager = null;
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == _networkManager.LocalClientId && !_networkManager.IsServer && State != NetworkSessionState.InGame)
            {
                SetState(NetworkSessionState.Connecting, $"Room {CurrentRoom.RoomCode} accepted connection");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
			_approvedPlayers.Remove(clientId);

            if (clientId != _networkManager.LocalClientId || _shutdownRequested)
            {
                return;
            }

			HandleLocalDisconnect();
		}

		private void HandleLocalDisconnect()
		{
			if (_shutdownRequested || _recoveringFromDisconnect)
			{
				return;
			}

			bool inMatch = State is NetworkSessionState.Loading or NetworkSessionState.InGame;
			string disconnectReason = _networkManager.DisconnectReason;
			LastFailure = inMatch && Mode == NetworkSessionMode.Client
				? SessionFailure.HostLeft
				: SessionFailureReasons.FromDisconnectReason(disconnectReason, Mode == NetworkSessionMode.Client);
            string reason = string.IsNullOrWhiteSpace(disconnectReason)
                ? "Connection to room closed"
                : disconnectReason;

            SetState(NetworkSessionState.Failed, reason);

			if (inMatch)
            {
				_ = ReturnToMenuAfterDisconnectAsync(reason);
            }
        }

        private async Awaitable ReturnToMenuAfterDisconnectAsync(string reason)
        {
            _recoveringFromDisconnect = true;
            try
            {
                SessionFailure failure = LastFailure;
                await ShutdownAndReturnToMenuAsync();
                LastFailure = failure;
                SetState(NetworkSessionState.Failed, $"Connection lost: {reason}");
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _recoveringFromDisconnect = false;
            }
        }

		private void ApproveRoomConnection(
			NetworkManager.ConnectionApprovalRequest request,
			NetworkManager.ConnectionApprovalResponse response)
		{
			_approvalHandler.ApproveRoomConnection(request, response, CurrentRoom);
			if (!response.Approved || request.ClientNetworkId == NetworkManager.ServerClientId)
			{
				return;
			}

			ISession session = _sessionConnector.ActiveSession;
			if (!ConnectionApprovalHandler.TryDeserializePayload(
					request.Payload, out ConnectionApprovalHandler.RoomConnectionPayload payload) ||
				string.IsNullOrEmpty(payload.PlayerId) ||
				payload.PlayerId.Length > 128 ||
				payload.PlayerId == _sessionConnector.LocalIdentity ||
				(session != null && !session.Players.Any(player => player.Id == payload.PlayerId)) ||
				_approvedPlayers.Any(player =>
					player.Value == payload.PlayerId && _networkManager.ConnectedClients.ContainsKey(player.Key)))
			{
				ConnectionApprovalHandler.Reject(response, SessionFailureReasons.InvalidPayload);
				return;
			}

			_approvedPlayers[request.ClientNetworkId] = payload.PlayerId;
		}

        private void OnSessionStopped(bool _)
        {
            _progressGeneration++;
            IsSceneEventInProgress = false;

			if (Mode == NetworkSessionMode.Client && State is (NetworkSessionState.Loading or NetworkSessionState.InGame))
			{
				HandleLocalDisconnect();
			}
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
			if (_shutdownRequested)
			{
				return;
			}

            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.SynchronizeComplete when sceneEvent.ClientId == _networkManager.LocalClientId && !_networkManager.IsServer:
                    IsSceneEventInProgress = false;
                    SetProgress(1f);
                    if (SceneManager.GetActiveScene().name == SceneNames.Game)
                    {
                        SetState(NetworkSessionState.InGame, $"Room {CurrentRoom.RoomCode}: game synchronized");
                    }
                    break;

                case SceneEventType.Load:
                    IsSceneEventInProgress = true;
                    SetProgress(ProgressSceneLoading);
                    SetState(
                        NetworkSessionState.Loading,
                        $"Loading {sceneEvent.SceneName}");
                    _ = TrackAsyncOperationAsync(sceneEvent.AsyncOperation);
                    break;

                case SceneEventType.LoadComplete:
                    SetProgress(Mathf.Max(LoadingProgress, ProgressSceneLoaded));
                    break;

                case SceneEventType.LoadEventCompleted:
                    IsSceneEventInProgress = false;
                    SetProgress(1f);
                    if (sceneEvent.SceneName == SceneNames.Game)
                    {
                        SetState(NetworkSessionState.InGame, $"Room {CurrentRoom.RoomCode}: game loaded");
                    }
                    else
                    {
                        SetState(NetworkSessionState.Loading, "Preparing game world");
                    }

                    break;
            }
        }

        private async Awaitable TrackAsyncOperationAsync(AsyncOperation operation)
        {
            if (operation == null)
            {
                return;
            }

            int generation = ++_progressGeneration;

            try
            {
                while (!operation.isDone && generation == _progressGeneration)
                {
                    float normalized = Mathf.Clamp01(operation.progress / ProgressTrackMax);
                    SetProgress(Mathf.Lerp(ProgressSceneLoading, ProgressTrackMax, normalized));
                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void SetProgress(float progress)
        {
            LoadingProgress = Mathf.Clamp01(progress);
            LoadingProgressChanged?.Invoke(LoadingProgress);
        }

        private void SetState(NetworkSessionState state, string status)
        {
            if (state != NetworkSessionState.Failed)
            {
                LastFailure = SessionFailure.None;
            }
            else if (LastFailure == SessionFailure.None)
            {
                LastFailure = SessionFailure.ConnectionFailed;
            }

            State = state;
            Status = status;
            StateChanged?.Invoke(state, status);
        }

    }
}
