using System;
using System.Text;
using Planet_IO;
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
    public sealed class NetworkSessionService :
        INetworkSessionService,
        IStartable,
        IDisposable
    {
        private const float ClientConnectionTimeoutSeconds = 8f;

        private readonly NetworkManager _networkManager;
        private readonly IPlayerProfileService _playerProfileService;
        private NetworkSceneManager _networkSceneManager;
        private int _progressGeneration;
        private bool _subscribed;
        private bool _shutdownRequested;
        private bool _recoveringFromDisconnect;
        private bool _ugsInitialized;

        public NetworkSessionService(
            NetworkManager networkManager,
            IPlayerProfileService playerProfileService)
        {
            _networkManager = networkManager
                ?? throw new ArgumentNullException(nameof(networkManager));
            _playerProfileService = playerProfileService
                ?? throw new ArgumentNullException(nameof(playerProfileService));
        }

        public event Action<float> LoadingProgressChanged;
        public event Action<NetworkSessionState, string> StateChanged;

        public NetworkSessionState State { get; private set; } =
            NetworkSessionState.Offline;
        public NetworkSessionMode Mode { get; private set; } =
            NetworkSessionMode.None;
        public RoomConnectionSettings CurrentRoom { get; private set; } =
            RoomConnectionSettings.Default;
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
            SetState(
                NetworkSessionState.StartingHost,
                "Creating room...");

            try
            {
                await EnsureUgsInitializedAsync();

                Allocation allocation = await RelayService.Instance
                    .CreateAllocationAsync(maxPlayers - 1);

                string joinCode = await RelayService.Instance
                    .GetJoinCodeAsync(allocation.AllocationId);

                ConfigureTransportForRelay(allocation);

                CurrentRoom = new RoomConnectionSettings(
                    joinCode,
                    maxPlayers);

                _networkManager.ConnectionApprovalCallback =
                    ApproveRoomConnection;

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

                SetState(
                    NetworkSessionState.StartingHost,
                    $"Room created. Code: {joinCode}");

                return LoadNetworkScene(SceneNames.Loading);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
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
            SetState(
                NetworkSessionState.StartingClient,
                $"Connecting to {relayJoinCode}...");

            try
            {
                await EnsureUgsInitializedAsync();

                JoinAllocation joinAllocation = await RelayService.Instance
                    .JoinAllocationAsync(relayJoinCode.Trim());

                ConfigureTransportForRelay(joinAllocation);

                CurrentRoom = new RoomConnectionSettings(
                    relayJoinCode.Trim(),
                    RoomRules.DefaultMaxPlayers);

                if (!_networkManager.StartClient())
                {
                    FailStart("NetworkManager rejected client start");
                    return false;
                }

                SubscribeSceneManager();
                SetState(
                    NetworkSessionState.Connecting,
                    $"Joining room {relayJoinCode}");

                float connectionDeadline =
                    Time.realtimeSinceStartup + ClientConnectionTimeoutSeconds;

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

                    await Awaitable.NextFrameAsync(
                        Application.exitCancellationToken);
                }

                if (_networkManager.IsConnectedClient)
                {
                    return true;
                }

                string failureReason = string.IsNullOrWhiteSpace(
                    _networkManager.DisconnectReason)
                    ? $"Room did not respond within {ClientConnectionTimeoutSeconds:0}s."
                    : _networkManager.DisconnectReason;

                await StopNetworkManagerAsync();
                FailStart(failureReason);
                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                FailStart($"Connection error: {exception.Message}");
                return false;
            }
        }

        public async Awaitable<bool> StartSinglePlayerAsync()
        {
            RoomConnectionSettings singlePlayerRoom = new(
                "SOLO",
                1);

            if (!CanStartSession())
            {
                return false;
            }

            CurrentRoom = singlePlayerRoom;
            Mode = NetworkSessionMode.SinglePlayer;
            SetState(
                NetworkSessionState.StartingSinglePlayer,
                "Starting single player");
            _networkManager.ConnectionApprovalCallback =
                ApproveSinglePlayerConnection;

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
                FailStart(
                    "Single player did not transition to Listening state");
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
                    await Awaitable.NextFrameAsync(
                        Application.exitCancellationToken);
                }

                await Awaitable.WaitForSecondsAsync(
                    0.25f,
                    Application.exitCancellationToken);
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
            SetState(
                NetworkSessionState.ShuttingDown,
                "Shutting down network session");

            await StopNetworkManagerAsync();
            _networkManager.ConnectionApprovalCallback = null;

            IsSceneEventInProgress = false;
            SetProgress(0f);

            try
            {
                await SceneManager.LoadSceneAsync(
                    SceneNames.Menu,
                    LoadSceneMode.Single);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            _shutdownRequested = false;
            Mode = NetworkSessionMode.None;
            CurrentRoom = RoomConnectionSettings.Default;
            SetState(
                NetworkSessionState.Offline,
                "Ready to connect");
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
                Debug.LogException(exception);
                throw;
            }
        }

        private void ConfigureTransportForRelay(Allocation allocation)
        {
            if (_networkManager.NetworkConfig.NetworkTransport is not
                UnityTransport transport)
            {
                throw new InvalidOperationException(
                    "Relay requires Unity Transport");
            }

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "dtls"));
        }

        private void ConfigureTransportForRelay(JoinAllocation joinAllocation)
        {
            if (_networkManager.NetworkConfig.NetworkTransport is not
                UnityTransport transport)
            {
                throw new InvalidOperationException(
                    "Relay requires Unity Transport");
            }

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
        }

        private void ApproveRoomConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                Approve(response);
                return;
            }

            if (!TryDeserializePayload(
                    request.Payload,
                    out RoomConnectionPayload payload))
            {
                Reject(response, "Invalid connection payload.");
                return;
            }

            if (!string.Equals(
                    payload.Protocol,
                    RoomRules.ProtocolVersion,
                    StringComparison.Ordinal))
            {
                Reject(response, "Client version does not match room version.");
                return;
            }

            if (_networkManager.ConnectedClientsIds.Count >=
                CurrentRoom.MaxPlayers)
            {
                Reject(response, "Room is full.");
                return;
            }

            Approve(response);
        }

        private static void ApproveSinglePlayerConnection(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId == NetworkManager.ServerClientId)
            {
                Approve(response);
            }
            else
            {
                Reject(response, "This session is running in single player mode.");
            }
        }

        private static void Approve(
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = string.Empty;
        }

        private static void Reject(
            NetworkManager.ConnectionApprovalResponse response,
            string reason)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = reason;
        }

        private bool CanStartSession()
        {
            if (_networkManager.IsListening ||
                _networkManager.ShutdownInProgress)
            {
                SetState(
                    NetworkSessionState.Failed,
                    "Network session is already running or shutting down");
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
                    await Awaitable.NextFrameAsync(
                        Application.exitCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
        }

        private static byte[] SerializePayload(RoomConnectionPayload payload)
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        }

        private static bool TryDeserializePayload(
            byte[] bytes,
            out RoomConnectionPayload payload)
        {
            payload = null;
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<RoomConnectionPayload>(
                    Encoding.UTF8.GetString(bytes));
                return payload != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Awaitable<bool> WaitOneFrameAsync()
        {
            try
            {
                await Awaitable.NextFrameAsync(
                    Application.exitCancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private bool LoadNetworkScene(string sceneName)
        {
            if (!_networkManager.IsServer ||
                _networkManager.SceneManager == null)
            {
                SetState(
                    NetworkSessionState.Failed,
                    "NetworkSceneManager is not ready yet");
                return false;
            }

            SceneEventProgressStatus result =
                _networkManager.SceneManager.LoadScene(
                    sceneName,
                    LoadSceneMode.Single);

            if (result != SceneEventProgressStatus.Started)
            {
                SetState(
                    NetworkSessionState.Failed,
                    $"Failed to load scene {sceneName}: {result}");
                return false;
            }

            IsSceneEventInProgress = true;
            SetProgress(0.02f);
            SetState(
                NetworkSessionState.Loading,
                $"Loading scene {sceneName}");
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
            _networkManager.OnClientStopped += OnClientStopped;
            _networkManager.OnServerStopped += OnServerStopped;

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
            _networkManager.OnClientStopped -= OnClientStopped;
            _networkManager.OnServerStopped -= OnServerStopped;

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
            if (clientId == _networkManager.LocalClientId &&
                !_networkManager.IsServer)
            {
                SetState(
                    NetworkSessionState.Connecting,
                    $"Room {CurrentRoom.RoomCode} accepted connection");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId != _networkManager.LocalClientId ||
                _shutdownRequested)
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(
                _networkManager.DisconnectReason)
                ? "Connection to room closed"
                : _networkManager.DisconnectReason;
            bool shouldReturnToMenu =
                State is NetworkSessionState.Loading or
                    NetworkSessionState.InGame;
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
                SetState(
                    NetworkSessionState.Failed,
                    $"Connection lost: {reason}");
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
            finally
            {
                _recoveringFromDisconnect = false;
            }
        }

        private void OnClientStopped(bool wasHost)
        {
            IsSceneEventInProgress = false;
        }

        private void OnServerStopped(bool wasHost)
        {
            IsSceneEventInProgress = false;
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            switch (sceneEvent.SceneEventType)
            {
                case SceneEventType.Load:
                    IsSceneEventInProgress = true;
                    SetProgress(0.04f);
                    SetState(
                        NetworkSessionState.Loading,
                        $"Loading {sceneEvent.SceneName}");
                    _ = TrackAsyncOperationAsync(sceneEvent.AsyncOperation);
                    break;

                case SceneEventType.LoadComplete:
                    SetProgress(Mathf.Max(LoadingProgress, 0.94f));
                    break;

                case SceneEventType.LoadEventCompleted:
                    IsSceneEventInProgress = false;
                    SetProgress(1f);
                    SetState(
                        sceneEvent.SceneName == SceneNames.Game
                            ? NetworkSessionState.InGame
                            : NetworkSessionState.Loading,
                        sceneEvent.SceneName == SceneNames.Game
                            ? $"Room {CurrentRoom.RoomCode}: game loaded"
                            : "Preparing game world");
                    break;
            }
        }

        private async Awaitable TrackAsyncOperationAsync(
            AsyncOperation operation)
        {
            if (operation == null)
            {
                return;
            }

            int generation = ++_progressGeneration;

            try
            {
                while (!operation.isDone &&
                       generation == _progressGeneration)
                {
                    float normalized =
                        Mathf.Clamp01(operation.progress / 0.9f);
                    SetProgress(
                        Mathf.Lerp(0.04f, 0.9f, normalized));
                    await Awaitable.NextFrameAsync(
                        Application.exitCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
        }

        private void SetProgress(float progress)
        {
            LoadingProgress = Mathf.Clamp01(progress);
            LoadingProgressChanged?.Invoke(LoadingProgress);
        }

        private void SetState(
            NetworkSessionState state,
            string status)
        {
            State = state;
            Status = status;
            StateChanged?.Invoke(state, status);
        }

        [Serializable]
        private sealed class RoomConnectionPayload
        {
            public string Protocol;
            public string Nickname;
        }
    }
}
