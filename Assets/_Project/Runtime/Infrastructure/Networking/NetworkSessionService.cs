using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class NetworkSessionService : INetworkSessionService, IStartable, IDisposable
    {
        private const float ClientConnectionTimeoutSeconds = 8f;
        private const float ProgressInitial = 0.02f;
        private const float ProgressSceneLoading = 0.04f;
        private const float ProgressSceneLoaded = 0.94f;
        private const float ProgressTrackMax = 0.9f;

        private readonly NetworkManager _networkManager;
        private readonly ConnectionApprovalHandler _approvalHandler;
        private NetworkSceneManager _networkSceneManager;
		private readonly IPlayerProfileService _playerProfileService;

        private int _progressGeneration;
        private bool _subscribed;
        private bool _shutdownRequested;
        private bool _recoveringFromDisconnect;
        private bool _ugsInitialized;

        public NetworkSessionService(NetworkManager networkManager, IPlayerProfileService playerProfileService)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _approvalHandler = new ConnectionApprovalHandler(networkManager);
			_playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
        }

        public event Action<float> LoadingProgressChanged;
        public event Action<NetworkSessionState, string> StateChanged;

        public NetworkSessionState State { get; private set; } = NetworkSessionState.Offline;
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

        public async Awaitable<bool> StartHostAsync(int maxPlayers)
        {
            if (!CanStartSession())
            {
                return false;
            }

            Mode = NetworkSessionMode.Host;
            SetState(NetworkSessionState.StartingHost, "Creating room...");

            try
            {
                await EnsureUgsInitializedAsync();

                Allocation allocation = await RelayService.Instance
                    .CreateAllocationAsync(maxPlayers - 1);

                string joinCode = await RelayService.Instance
                    .GetJoinCodeAsync(allocation.AllocationId);

                ConfigureTransportForRelay(allocation);

                CurrentRoom = new RoomConnectionSettings(joinCode, maxPlayers);

                _networkManager.ConnectionApprovalCallback = (request, response) =>
                        _approvalHandler.ApproveRoomConnection(request, response, CurrentRoom);

                if (!_networkManager.StartHost())
                {
                    FailStart("NetworkManager rejected room start");
                    return false;
                }

                SubscribeSceneManager();
                if (!await WaitOneFrameAsync())
                {
                    return false;
                }

                if (!_networkManager.IsListening || !_networkManager.IsServer)
                {
                    FailStart("Room did not transition to Listening state");
                    return false;
                }

                SetState(NetworkSessionState.StartingHost, $"Room created. Code: {joinCode}");

                return LoadNetworkScene(SceneNames.Loading);
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
                FailStart($"Failed to create room: {exception.Message}");
                return false;
            }
        }

        public async Awaitable<bool> StartClientAsync(string relayJoinCode)
        {
            if (!CanStartSession())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(relayJoinCode))
            {
                FailStart("Room code cannot be empty");
                return false;
            }

            Mode = NetworkSessionMode.Client;
            _networkManager.ConnectionApprovalCallback = null;
            SetState(NetworkSessionState.StartingClient, $"Connecting to {relayJoinCode}...");

            try
            {
                await EnsureUgsInitializedAsync();

                JoinAllocation joinAllocation = await RelayService.Instance
                    .JoinAllocationAsync(relayJoinCode.Trim());

                ConfigureTransportForRelay(joinAllocation);

                CurrentRoom = new RoomConnectionSettings(relayJoinCode.Trim(), RoomRules.DefaultMaxPlayers);

				ConnectionApprovalHandler.RoomConnectionPayload payload = new ConnectionApprovalHandler.RoomConnectionPayload
				{
					Protocol = RoomRules.ProtocolVersion,
					Nickname = _playerProfileService.Nickname
				};

				_networkManager.NetworkConfig.ConnectionData = ConnectionApprovalHandler.SerializePayload(payload);

                if (!_networkManager.StartClient())
                {
                    FailStart("NetworkManager rejected client start");
                    return false;
                }

                SubscribeSceneManager();
                SetState(NetworkSessionState.Connecting, $"Joining room {relayJoinCode}");

                float connectionDeadline = Time.realtimeSinceStartup + ClientConnectionTimeoutSeconds;

                while (Time.realtimeSinceStartup < connectionDeadline)
                {
                    if (_networkManager.IsConnectedClient)
                    {
                        return true;
                    }

                    if (!_networkManager.IsListening)
                    {
                        break;
                    }

                    await Awaitable.NextFrameAsync();
                }

                if (_networkManager.IsConnectedClient)
                {
                    return true;
                }

                string failureReason = string.IsNullOrWhiteSpace(_networkManager.DisconnectReason)
                    ? $"Room did not respond within {ClientConnectionTimeoutSeconds:0}s."
                    : _networkManager.DisconnectReason;

                await StopNetworkManagerAsync();
                FailStart(failureReason);
                return false;
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
                FailStart($"Connection error: {exception.Message}");
                return false;
            }
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
            SetState(NetworkSessionState.StartingSinglePlayer, "Starting single player");
            _networkManager.ConnectionApprovalCallback = ConnectionApprovalHandler.ApproveSinglePlayerConnection;

            if (!_networkManager.StartHost())
            {
                FailStart("Failed to start single player");
                return false;
            }

            SubscribeSceneManager();
            if (!await WaitOneFrameAsync())
            {
                return false;
            }

            if (!_networkManager.IsListening || !_networkManager.IsServer)
            {
                FailStart("Single player did not transition to Listening state");
                return false;
            }

            return LoadNetworkScene(SceneNames.Loading);
        }

        public async Awaitable ContinueToGameAsync()
        {
            if (!IsServer)
            {
                return;
            }

            try
            {
                while (IsSceneEventInProgress)
                {
                    await Awaitable.NextFrameAsync();
                }

                await Awaitable.NextFrameAsync();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            LoadNetworkScene(SceneNames.Game);
        }

        public async Awaitable ShutdownAndReturnToMenuAsync()
        {
            if (_shutdownRequested)
            {
                return;
            }

            _shutdownRequested = true;
            SetState(NetworkSessionState.ShuttingDown, "Shutting down network session");

            await StopNetworkManagerAsync();
            _networkManager.ConnectionApprovalCallback = null;
			_networkManager.NetworkConfig.ConnectionData = Array.Empty<byte>();

            IsSceneEventInProgress = false;
            SetProgress(0f);

            try
            {
                await SceneManager.LoadSceneAsync(SceneNames.Menu, LoadSceneMode.Single);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _shutdownRequested = false;
            Mode = NetworkSessionMode.None;
            CurrentRoom = RoomConnectionSettings.Default;
            SetState(NetworkSessionState.Offline, "Ready to connect");
        }

        public void Dispose()
        {
            _networkManager.ConnectionApprovalCallback = null;
            Unsubscribe();
        }

        private async Awaitable EnsureUgsInitializedAsync()
        {
            if (_ugsInitialized)
            {
                return;
            }

            try
            {
                await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                _ugsInitialized = true;
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
                throw;
            }
        }

        private UnityTransport GetRelayTransport()
        {
            if (_networkManager.NetworkConfig.NetworkTransport is not UnityTransport transport)
            {
                throw new InvalidOperationException("Relay requires Unity Transport");
            }

            return transport;
        }

        private void ConfigureTransportForRelay(Allocation allocation)
        {
            GetRelayTransport().SetRelayServerData(allocation.ToRelayServerData("dtls"));
        }

        private void ConfigureTransportForRelay(JoinAllocation joinAllocation)
        {
            GetRelayTransport().SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));
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
            _networkManager.ConnectionApprovalCallback = null;
			_networkManager.NetworkConfig.ConnectionData = Array.Empty<byte>();

            SetState(NetworkSessionState.Failed, reason);
        }

        private async Awaitable StopNetworkManagerAsync()
        {
            if (_networkManager.IsListening)
            {
                UnsubscribeSceneManager();
                _networkManager.Shutdown();
            }

            try
            {
                while (_networkManager.ShutdownInProgress)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
            catch (OperationCanceledException)
            {
				LoggerIO.LogError("Application is closing");
            }
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
            if (clientId == _networkManager.LocalClientId && !_networkManager.IsServer)
            {
                SetState(NetworkSessionState.Connecting, $"Room {CurrentRoom.RoomCode} accepted connection");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId != _networkManager.LocalClientId || _shutdownRequested)
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(_networkManager.DisconnectReason)
                ? "Connection to room closed"
                : _networkManager.DisconnectReason;

            bool shouldReturnToMenu = State is NetworkSessionState.Loading or NetworkSessionState.InGame;
            SetState(NetworkSessionState.Failed, reason);

            if (shouldReturnToMenu && !_recoveringFromDisconnect)
            {
                _ = RecoverFromDisconnectAsync(reason);
            }
        }

        private async Awaitable RecoverFromDisconnectAsync(string reason)
        {
            _recoveringFromDisconnect = true;
            try
            {
                await ShutdownAndReturnToMenuAsync();
                SetState(NetworkSessionState.Failed, $"Connection lost: {reason}");
            }
            catch (OperationCanceledException)
            {
				LoggerIO.LogError("Application is closing");
            }
            finally
            {
                _recoveringFromDisconnect = false;
            }
        }

        private void OnSessionStopped(bool wasHost)
        {
            IsSceneEventInProgress = false;
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
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
                    SetState(sceneEvent.SceneName == SceneNames.Game
                            ? NetworkSessionState.InGame
                            : NetworkSessionState.Loading,

                        sceneEvent.SceneName == SceneNames.Game
                            ? $"Room {CurrentRoom.RoomCode}: game loaded"
                            : "Preparing game world");
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
				LoggerIO.LogError("Application is closing");
            }
        }

        private void SetProgress(float progress)
        {
            LoadingProgress = Mathf.Clamp01(progress);
            LoadingProgressChanged?.Invoke(LoadingProgress);
        }

        private void SetState(NetworkSessionState state, string status)
        {
            State = state;
            Status = status;
            StateChanged?.Invoke(state, status);
        }

    }
}
