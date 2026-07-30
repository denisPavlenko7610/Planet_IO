using System;
using System.Threading;
using PlanetIO.Core.Contracts.Loading;
using PlanetIO.ObjectPool;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.Application
{
    public sealed class GameFlowService : IGameStateService, IAsyncStartable, ITickable, IDisposable
    {
        private readonly PointSpawner _pointSpawner;
        private readonly CometSpawner _cometSpawner;
        private readonly EnemySpawner _enemySpawner;
        private readonly ObjectPool<Point> _pointsPool;
        private readonly ObjectPool<Comet> _cometsPool;
        private readonly ObjectPool<Enemy> _enemyPool;
        private readonly NetworkManager _networkManager;
        private readonly IGameLoadingView _loadingView;
        private bool _worldInitialized;
        private bool _disposed;

        public GameFlowService(
            PointSpawner pointSpawner,
            CometSpawner cometSpawner,
            EnemySpawner enemySpawner,
            ObjectPool<Point> pointsPool,
            ObjectPool<Comet> cometsPool,
            ObjectPool<Enemy> enemyPool,
            NetworkManager networkManager,
            IGameLoadingView loadingView)
        {
            _pointSpawner = pointSpawner ?? throw new ArgumentNullException(nameof(pointSpawner));
            _cometSpawner = cometSpawner ?? throw new ArgumentNullException(nameof(cometSpawner));
            _enemySpawner = enemySpawner ?? throw new ArgumentNullException(nameof(enemySpawner));
            _pointsPool = pointsPool ?? throw new ArgumentNullException(nameof(pointsPool));
            _cometsPool = cometsPool ?? throw new ArgumentNullException(nameof(cometsPool));
            _enemyPool = enemyPool ?? throw new ArgumentNullException(nameof(enemyPool));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _loadingView = loadingView ?? throw new ArgumentNullException(nameof(loadingView));
        }

        public event Action<GameState, GameState> StateChanged;

        public GameState State { get; private set; } = GameState.None;
        public bool IsGameplayActive => State == GameState.Playing;

        public async Awaitable StartAsync(CancellationToken cancellation = default)
        {
            TransitionTo(GameState.Initializing);

            if (!await InitializeWorldAsync(cancellation))
            {
                return;
            }

            TransitionTo(GameState.WaitingForPlayers);
        }

        public void Tick()
        {
            if (_disposed)
            {
                return;
            }

            switch (State)
            {
                case GameState.WaitingForPlayers when IsSessionReady():
                    TransitionTo(GameState.Playing);
                    break;

                case GameState.Playing when !IsSessionAlive():
                    TransitionTo(GameState.GameOver);
                    break;
            }
        }

        public void FinishGame()
        {
            if (State is GameState.WaitingForPlayers or GameState.Playing)
            {
                TransitionTo(GameState.GameOver);
            }
        }

        public void BeginShutdown()
        {
            if (State != GameState.ShuttingDown)
            {
                TransitionTo(GameState.ShuttingDown);
            }
        }

        public void Dispose()
        {
            _disposed = true;

            if (State != GameState.ShuttingDown)
            {
                TransitionTo(GameState.ShuttingDown);
            }
        }

        private async Awaitable<bool> InitializeWorldAsync(CancellationToken cancellation)
        {
            if (_worldInitialized)
            {
                return true;
            }

            if (!_networkManager.IsServer)
            {
                return !_disposed && _networkManager.IsListening;
            }

            if (!CanInitializeWorld())
            {
                return false;
            }

            _pointSpawner.Initialize(_pointsPool);
            _cometSpawner.Initialize(_cometsPool);
            _enemySpawner.Initialize(_enemyPool);

            const int spawnBatchSize = 5;
            int pointsCount = _pointsPool.Capacity;
            int cometsCount = _cometsPool.Capacity;
            int enemiesCount = _enemyPool.Capacity;
            int maxCount = Mathf.Max(pointsCount, cometsCount, enemiesCount);

            for (int i = 0; i < maxCount; i++)
            {
                cancellation.ThrowIfCancellationRequested();

                if (!CanInitializeWorld())
                {
                    return false;
                }

                if (i < pointsCount)
                {
                    _pointSpawner.CreateObject();
                }

                if (i < cometsCount)
                {
                    _cometSpawner.CreateObject();
                }

                if (i < enemiesCount)
                {
                    _enemySpawner.CreateObject();
                }

                if (i % spawnBatchSize == spawnBatchSize - 1)
                {
                    await Awaitable.NextFrameAsync(cancellation);
                }
            }

            _worldInitialized = true;
            return true;
        }

        private bool CanInitializeWorld() =>
            !_disposed &&
            _networkManager.IsListening &&
            _networkManager.IsServer;

        private bool IsSessionReady()
        {
            if (!IsSessionAlive())
            {
                return false;
            }

            return _networkManager.IsServer
                ? _networkManager.ConnectedClientsList.Count > 0
                : _networkManager.IsConnectedClient;
        }

        private bool IsSessionAlive()
        {
            return _networkManager.IsListening;
        }

        private void TransitionTo(GameState nextState)
        {
            if (State == nextState)
            {
                return;
            }

            if (!IsValidTransition(State, nextState))
            {
                throw new InvalidOperationException(
                    $"Invalid game-state transition: {State} -> {nextState}.");
            }

            GameState previousState = State;
            State = nextState;
            StateChanged?.Invoke(previousState, nextState);
        }

        private static bool IsValidTransition(GameState current, GameState next)
        {
            if (next == GameState.ShuttingDown)
            {
                return current != GameState.ShuttingDown;
            }

            return (current, next) switch
            {
                (GameState.None, GameState.Initializing) => true,
                (GameState.Initializing, GameState.WaitingForPlayers) => true,
                (GameState.WaitingForPlayers, GameState.Playing) => true,
                (GameState.WaitingForPlayers, GameState.GameOver) => true,
                (GameState.Playing, GameState.GameOver) => true,
                _ => false
            };
        }
    }
}
