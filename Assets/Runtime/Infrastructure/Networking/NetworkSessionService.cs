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
        private readonly NetworkManager _networkManager;
        private NetworkSceneManager _networkSceneManager;
        private int _progressGeneration;
        private bool _subscribed;
        private bool _shutdownRequested;

        public NetworkSessionService(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public event Action<float> LoadingProgressChanged;
        public event Action<NetworkSessionState, string> StateChanged;

        public NetworkSessionState State { get; private set; } = NetworkSessionState.Offline;
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

            SetState(NetworkSessionState.StartingHost, "Запуск хоста");
            _networkManager.ConnectionApprovalCallback = ApproveConnectionWithoutPlayer;

            if (!_networkManager.StartHost())
            {
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
                SetState(NetworkSessionState.Failed, "Хост не перешёл в состояние Listening");
                return false;
            }

            return LoadNetworkScene(SceneNames.Loading);
        }

        public bool StartClient()
        {
            if (!CanStartSession())
            {
                return false;
            }

            SetState(NetworkSessionState.StartingClient, "Подключение к хосту");

            if (!_networkManager.StartClient())
            {
                SetState(NetworkSessionState.Failed, "NetworkManager отклонил запуск клиента");
                return false;
            }

            SubscribeSceneManager();
            SetState(NetworkSessionState.Connecting, "Ожидание ответа хоста");
            return true;
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

            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
                try
                {
                    await Awaitable.NextFrameAsync(Application.exitCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            if (_networkManager != null)
            {
                _networkManager.ConnectionApprovalCallback = null;
            }

            IsSceneEventInProgress = false;
            SetProgress(0f);
            await SceneManager.LoadSceneAsync(SceneNames.Menu, LoadSceneMode.Single);
            _shutdownRequested = false;
            SetState(NetworkSessionState.Offline, "Готово к подключению");
        }

        public void Dispose()
        {
            if (_networkManager != null)
            {
                _networkManager.ConnectionApprovalCallback = null;
            }

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

        private bool CanStartSession()
        {
            if (_networkManager == null)
            {
                SetState(NetworkSessionState.Failed, "NetworkManager не зарегистрирован");
                return false;
            }

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
            if (_subscribed || _networkManager == null)
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
            if (!_subscribed || _networkManager == null)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback -= OnClientConnected;
            _networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            _networkManager.OnClientStopped -= OnClientStopped;
            _networkManager.OnServerStopped -= OnServerStopped;

            if (_networkSceneManager != null)
            {
                _networkSceneManager.OnSceneEvent -= OnSceneEvent;
                _networkSceneManager = null;
            }

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

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == _networkManager.LocalClientId && !_networkManager.IsServer)
            {
                SetState(NetworkSessionState.Connecting, "Подключено, синхронизация сцены");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (clientId != _networkManager.LocalClientId || _shutdownRequested)
            {
                return;
            }

            string reason = string.IsNullOrWhiteSpace(_networkManager.DisconnectReason)
                ? "Соединение с хостом закрыто"
                : _networkManager.DisconnectReason;
            SetState(NetworkSessionState.Failed, reason);
        }

        private void OnClientStopped(bool _)
        {
            IsSceneEventInProgress = false;
        }

        private void OnServerStopped(bool _)
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
                    TrackAsyncOperation(sceneEvent.AsyncOperation);
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

        private async void TrackAsyncOperation(AsyncOperation operation)
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
