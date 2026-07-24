using System;
using PlanetIO.Core.Attributes;
using PlanetIO.Core.Contracts.Loading;
using PlanetIO.Application;
using PlanetIO.ObjectPool;
using PlanetIO.UI.Hud;
using PlanetIO.Camera;
using PlanetIO.UI.Loading;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.DependencyInjection
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        public static GameLifetimeScope Instance { get; private set; }

        [Header("Pools")]
        [SerializeField, Assign(AssignMode.Scene)]
        private ObjectPool<Point> _pointsPool;

        [SerializeField, Assign(AssignMode.Scene)]
        private ObjectPool<Enemy> _enemyPool;

        [SerializeField, Assign(AssignMode.Scene)]
        private ObjectPool<Comet> _cometsPool;

        [Header("Spawner services")]
        [SerializeField, Assign(AssignMode.Scene)]
        private PointSpawner _pointSpawner;

        [SerializeField, Assign(AssignMode.Scene)]
        private CometSpawner _cometSpawner;

        [SerializeField, Assign(AssignMode.Scene)]
        private EnemySpawner _enemySpawner;

        [Header("Application")]
        [SerializeField] private NetworkObject _playerPrefab;

        [SerializeField, Assign(AssignMode.Scene)]
        private RestartGame _restartGame;

        [Header("UI")]
        [SerializeField, Assign(AssignMode.Scene)]
        private SessionHudView _sessionHudView;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_pointsPool).As<ObjectPool<Point>>();
            builder.RegisterComponent(_cometsPool).As<ObjectPool<Comet>>();
            builder.RegisterComponent(_enemyPool).As<ObjectPool<Enemy>>();

            builder.RegisterComponent(_pointSpawner)
                .AsSelf()
                .As<IRespawnService<Point>>()
                .As<ISpawnService<Point>>();

            builder.RegisterComponent(_cometSpawner)
                .AsSelf()
                .As<IRespawnService<Comet>>();

            builder.RegisterComponent(_enemySpawner)
                .AsSelf()
                .As<IRespawnService<Enemy>>();

            builder.RegisterComponent(_restartGame);

            if (_playerPrefab == null)
            {
                throw new InvalidOperationException(
                    "[GameLifetimeScope] _playerPrefab is not assigned in the Inspector. Assign a NetworkObject prefab.");
            }

            builder.RegisterInstance(_playerPrefab);

            builder.RegisterComponentInHierarchy<BordersTrigger>();

            builder.RegisterComponentInHierarchy<AccelerationButton>()
                .As<IBoostInput>();

            builder.RegisterEntryPoint<GameFlowService>()
                .AsSelf()
                .As<IGameStateService>();

            builder.RegisterEntryPoint<NetworkPlayerSpawner>();

            builder.RegisterComponentInHierarchy<ScoreText>()
                .As<IScoreView>();

            builder.RegisterComponentInHierarchy<PlayerCamera>();
            builder.RegisterEntryPoint<LocalPlayerProvider>()
                .As<ILocalPlayerProvider>();

            builder.RegisterEntryPoint<ScorePresenter>();
            builder.RegisterEntryPoint<DirectionArrowPresenter>();
            builder.RegisterEntryPoint<GameSessionHudPresenter>();

            builder.RegisterComponentInHierarchy<GameLoadingView>()
                .As<IGameLoadingView>();

            if (_sessionHudView == null)
            {
                throw new InvalidOperationException(
                    "[GameLifetimeScope] _sessionHudView is not assigned. Add a SessionHudView component to a GameObject in the Game scene.");
            }

            builder.RegisterComponent(_sessionHudView)
                .As<ISessionHudView>();
        }

        protected override LifetimeScope FindParent()
        {
            return ApplicationLifetimeScope.Instance;
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroy();
        }
    }
}
