using System;
using Planet_IO;
using Unity.Netcode;
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
        private const float ClientConnectionTimeoutSeconds = 3f;

        private readonly NetworkManager _networkManager;
        private NetworkSceneManager _networkSceneManager;
        private int _progressGeneration;
        private bool _subscribed;
        private bool _shutdownRequested;
        private bool _switchingToSinglePlayer;

        public NetworkSessionService(NetworkManager networkManager)
        {
            _networkManager = networkManager
                ?? throw new ArgumentNullException(nameof(networkManager));
        }

        public event Action<float> LoadingProgressChanged;
        public event Action<NetworkSessionState, string> StateChanged;

        public NetworkSessionState State { get; private set; } = NetworkSessionState.Offline;
        public NetworkSessionMode Mode { get; private set; } = NetworkSessionMode.None;
        public string Status { get; private set; } = "Готово к подключению";
        public float LoadingProgress { get; private set; }
        public bool IsServer => _networkManager != null && _networkManager.IsServer;
        public bool IsSceneEventInProgress { get; private set; }

        public void Start()
        {
            Subscribe();
        }

        public async Awaitable<bool> StartHostAsync()
        {
            if (!CanStartSession())
            {
                return false;
            }

            Mode = NetworkSessionMode.Host;
            SetState(NetworkSessionState.StartingHost, "Запуск хоста");
            _networkManager.ConnectionApprovalCallback = ApproveConnectionWithoutPlayer;

            if (!_networkManager.StartHost())
            {
                Mode = NetworkSessionMode.None;
                _networkManager.ConnectionApprovalCallback = null;
                SetState(NetworkSessionState.Failed, "NetworkManager отклонил запуск хоста");
                return false;
            }

            SubscribeSceneManager();

            try
            {
                await Awaitable.NextFrameAsync(Application.exitCancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            if (!_networkManager.IsListening || !_networkManager.IsServer)
            {
                Mode = NetworkSessionMode.None;
                SetState(NetworkSessionState.Failed, "Хост не перешёл в состояние Listening");
                return false;
            }

            return LoadNetworkScene(SceneNames.Loading);
        }

        public async Awaitable<bool> StartClientOrSinglePlayerAsync()
        {
            if (!CanStartSession())
            {
                return false;
            }

            Mode = NetworkSessionMode.Client;
            SetState(NetworkSessionState.StartingClient, "Подключение к хосту");

            if (!_networkManager.StartClient())
            {
                return await StartSinglePlayerFallbackAsync();
            }

            SubscribeSceneManager();
            SetState(NetworkSessionState.Connecting, "Ожидание ответа хоста");

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

                    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
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

            return await StartSinglePlayerFallbackAsync();
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
                    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
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
            SetState(NetworkSessionState.ShuttingDown, "Завершение сетевой сессии");

            if (_networkManager.IsListening)
            {
                UnsubscribeSceneManager();
                _networkManager.Shutdown();

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
                    return;
                }
            }

            _networkManager.ConnectionApprovalCallback = null;

            IsSceneEventInProgress = false;
            SetProgress(0f);
            await SceneManager.LoadSceneAsync(SceneNames.Menu, LoadSceneMode.Single);
            _shutdownRequested = false;
            Mode = NetworkSessionMode.None;
            SetState(NetworkSessionState.Offline, "Готово к подключению");
        }

        public void Dispose()
        {
            _networkManager.ConnectionApprovalCallback = null;
            Unsubscribe();
        }

        private static void ApproveConnectionWithoutPlayer(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            response.Approved = true;
            response.CreatePlayerObject = false;
            response.Pending = false;
        }

        private static void ApproveLocalPlayerOnly(
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            bool isLocalPlayer = request.ClientNetworkId == NetworkManager.ServerClientId;
            response.Approved = isLocalPlayer;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = isLocalPlayer
                ? string.Empty
                : "The session is running in single-player mode.";
        }

        private async Awaitable<bool> StartSinglePlayerFallbackAsync()
        {
            _switchingToSinglePlayer = true;
            SetState(
                NetworkSessionState.StartingSinglePlayer,
                "Хост недоступен, запуск одиночной игры");

            try
            {
                if (_networkManager.IsListening)
                {
                    UnsubscribeSceneManager();
                    _networkManager.Shutdown();
                }

                while (_networkManager.ShutdownInProgress)
                {
                    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
                }

                Mode = NetworkSessionMode.SinglePlayer;
                _networkManager.ConnectionApprovalCallback = ApproveLocalPlayerOnly;

                if (!_networkManager.StartHost())
                {
                    Mode = NetworkSessionMode.None;
                    _networkManager.ConnectionApprovalCallback = null;
                    SetState(
                        NetworkSessionState.Failed,
                        "Не удалось запустить одиночную игру");
                    return false;
                }

                SubscribeSceneManager();
                await Awaitable.NextFrameAsync(Application.exitCancellationToken);

                if (!_networkManager.IsListening || !_networkManager.IsServer)
                {
                    Mode = NetworkSessionMode.None;
                    SetState(
                        NetworkSessionState.Failed,
                        "Одиночная игра не перешла в состояние Listening");
                    return false;
                }

                return LoadNetworkScene(SceneNames.Loading);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                _switchingToSinglePlayer = false;
            }
        }

        private bool CanStartSession()
        {
            if (_networkManager.IsListening)
            {
                SetState(NetworkSessionState.Failed, "Сетевая сессия уже запущена");
                return false;
            }

            return true;
        }

        private bool LoadNetworkScene(string sceneName)
        {
            if (!_networkManager.IsServer || _networkManager.SceneManager == null)
            {
                SetState(NetworkSessionState.Failed, "NetworkSceneManager ещё не готов");
                return false;
            }

            SceneEventProgressStatus result = _networkManager.SceneManager.LoadScene(
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
            SetState(NetworkSessionState.Loading, $"Загрузка сцены {sceneName}");
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
            if (clientId == _networkManager.LocalClientId && !_networkManager.IsServer)
            {
                SetState(NetworkSessionState.Connecting, "Подключено, синхронизация сцены");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId != _networkManager.LocalClientId ||
                _shutdownRequested ||
                _switchingToSinglePlayer)
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(_networkManager.DisconnectReason)
                ? "Соединение с хостом закрыто"
                : _networkManager.DisconnectReason;
            SetState(NetworkSessionState.Failed, reason);
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
                            ? "Игра загружена"
                            : "Подготовка игрового мира");
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
                    float normalized = Mathf.Clamp01(operation.progress / 0.9f);
                    SetProgress(Mathf.Lerp(0.04f, 0.9f, normalized));
                    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
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

        private void SetState(NetworkSessionState state, string status)
        {
            State = state;
            Status = status;
            StateChanged?.Invoke(state, status);
        }
    }
}
