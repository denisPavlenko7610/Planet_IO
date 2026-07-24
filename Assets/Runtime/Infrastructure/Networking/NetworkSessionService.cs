using System;
using System.Text;
using Planet_IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
        private const string ListenOnAllInterfaces = "0.0.0.0";

        private readonly NetworkManager _networkManager;
        private readonly IPlayerProfileService _playerProfileService;
        private NetworkSceneManager _networkSceneManager;
        private int _progressGeneration;
        private bool _subscribed;
        private bool _shutdownRequested;
        private bool _recoveringFromDisconnect;

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
        public string Status { get; private set; } = "Готово к подключению";
        public float LoadingProgress { get; private set; }
        public bool IsServer => _networkManager != null && _networkManager.IsServer;
        public bool IsSceneEventInProgress { get; private set; }

        public void Start()
        {
            Subscribe();
        }

        public async Awaitable<bool> StartHostAsync(RoomConnectionSettings room)
        {
            if (!CanStartSession() || !TryConfigureRoom(room, true))
            {
                return false;
            }

            Mode = NetworkSessionMode.Host;
            SetState(
                NetworkSessionState.StartingHost,
                $"Создание комнаты {CurrentRoom.RoomCode}");
            _networkManager.ConnectionApprovalCallback = ApproveRoomConnection;

            if (!_networkManager.StartHost())
            {
                FailStart("NetworkManager отклонил запуск комнаты");
                return false;
            }

            SubscribeSceneManager();
            if (!await WaitOneFrameAsync())
            {
                return false;
            }

            if (!_networkManager.IsListening || !_networkManager.IsServer)
            {
                FailStart("Комната не перешла в состояние Listening");
                return false;
            }

            return LoadNetworkScene(SceneNames.Loading);
        }

        public async Awaitable<bool> StartClientAsync(RoomConnectionSettings room)
        {
            if (!CanStartSession() || !TryConfigureRoom(room, false))
            {
                return false;
            }

            Mode = NetworkSessionMode.Client;
            _networkManager.ConnectionApprovalCallback = null;
            SetState(
                NetworkSessionState.StartingClient,
                $"Подключение к {CurrentRoom.Address}:{CurrentRoom.Port}");

            if (!_networkManager.StartClient())
            {
                FailStart("NetworkManager отклонил запуск клиента");
                return false;
            }

            SubscribeSceneManager();
            SetState(
                NetworkSessionState.Connecting,
                $"Вход в комнату {CurrentRoom.RoomCode}");

            try
            {
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
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (_networkManager.IsConnectedClient)
            {
                return true;
            }

            string failureReason = string.IsNullOrWhiteSpace(
                _networkManager.DisconnectReason)
                ? $"Комната не ответила за {ClientConnectionTimeoutSeconds:0} сек."
                : _networkManager.DisconnectReason;

            await StopNetworkManagerAsync();
            FailStart(failureReason);
            return false;
        }

        public async Awaitable<bool> StartSinglePlayerAsync()
        {
            RoomConnectionSettings singlePlayerRoom = new(
                "SOLO",
                RoomRules.DefaultAddress,
                RoomRules.DefaultPort,
                1);

            if (!CanStartSession() ||
                !TryConfigureRoom(singlePlayerRoom, true))
            {
                return false;
            }

            Mode = NetworkSessionMode.SinglePlayer;
            SetState(
                NetworkSessionState.StartingSinglePlayer,
                "Запуск одиночной игры");
            _networkManager.ConnectionApprovalCallback =
                ApproveSinglePlayerConnection;

            if (!_networkManager.StartHost())
            {
                FailStart("Не удалось запустить одиночную игру");
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
                    "Одиночная игра не перешла в состояние Listening");
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
                "Завершение сетевой сессии");

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
                "Готово к подключению");
        }

        public void Dispose()
        {
            _networkManager.ConnectionApprovalCallback = null;
            Unsubscribe();
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
                Reject(response, "Некорректные данные подключения.");
                return;
            }

            if (!string.Equals(
                    payload.Protocol,
                    RoomRules.ProtocolVersion,
                    StringComparison.Ordinal))
            {
                Reject(response, "Версия клиента не совпадает с версией комнаты.");
                return;
            }

            if (!string.Equals(
                    RoomRules.NormalizeRoomCode(payload.RoomCode),
                    CurrentRoom.RoomCode,
                    StringComparison.Ordinal))
            {
                Reject(response, "Неверный код комнаты.");
                return;
            }

            if (_networkManager.ConnectedClientsIds.Count >=
                CurrentRoom.MaxPlayers)
            {
                Reject(response, "Комната заполнена.");
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
                Reject(response, "Эта сессия запущена в одиночном режиме.");
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

        private bool TryConfigureRoom(
            RoomConnectionSettings room,
            bool isHost)
        {
            if (!RoomRules.IsValidRoomCode(room.RoomCode))
            {
                SetState(
                    NetworkSessionState.Failed,
                    $"Код комнаты: {RoomRules.MinimumRoomCodeLength}–" +
                    $"{RoomRules.MaximumRoomCodeLength} букв или цифр");
                return false;
            }

            if (_networkManager.NetworkConfig.NetworkTransport is not
                UnityTransport transport)
            {
                SetState(
                    NetworkSessionState.Failed,
                    "Для комнат требуется Unity Transport");
                return false;
            }

            CurrentRoom = new RoomConnectionSettings(
                room.RoomCode,
                room.Address,
                room.Port,
                room.MaxPlayers);

            transport.SetConnectionData(
                CurrentRoom.Address,
                CurrentRoom.Port,
                isHost ? ListenOnAllInterfaces : null);
            _networkManager.NetworkConfig.ConnectionData =
                SerializePayload(new RoomConnectionPayload
                {
                    Protocol = RoomRules.ProtocolVersion,
                    RoomCode = CurrentRoom.RoomCode,
                    Nickname = NicknameRules.Normalize(
                        _playerProfileService.Nickname)
                });

            return true;
        }

        private bool CanStartSession()
        {
            if (_networkManager.IsListening ||
                _networkManager.ShutdownInProgress)
            {
                SetState(
                    NetworkSessionState.Failed,
                    "Сетевая сессия уже запущена или завершается");
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
                    "NetworkSceneManager ещё не готов");
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
                    $"Не удалось загрузить сцену {sceneName}: {result}");
                return false;
            }

            IsSceneEventInProgress = true;
            SetProgress(0.02f);
            SetState(
                NetworkSessionState.Loading,
                $"Загрузка сцены {sceneName}");
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
                    $"Комната {CurrentRoom.RoomCode} приняла подключение");
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
                ? "Соединение с комнатой закрыто"
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
                    $"Соединение потеряно: {reason}");
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
                        $"Загрузка {sceneEvent.SceneName}");
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
                            ? $"Комната {CurrentRoom.RoomCode}: игра загружена"
                            : "Подготовка игрового мира");
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
            public string RoomCode;
            public string Nickname;
        }
    }
}
