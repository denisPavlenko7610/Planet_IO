using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Planet_IO;
using Planet_IO.Camera;
using Planet_IO.ObjectPool;
using RDTools.AutoAttach;
using UnityEngine;
using Zenject;

namespace PlanetIO_Core
{
    public class SceneInstaller : MonoInstaller
    {
        [Header("Pool and Spawner")] [SerializeField, Attach(Attach.Scene)]
        private ObjectPool<Point> _pointsPool;
        
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<Enemy> _enemyPool;
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<Comet> _cometsPool;

        [SerializeField, Attach(Attach.Scene)] private Spawner<Point> _pointsSpawner;
        [SerializeField, Attach(Attach.Scene)] private Spawner<Comet> _cometSpawner;
        [SerializeField, Attach(Attach.Scene)] private Spawner<Enemy> _enemySpawner;
        
        private PlayerMovement _playerMovement;

        private Player _player;

        [Header("logics Spawner")] [SerializeField, Attach(Attach.Scene)]
        private PointsSpawnerLogics _pointsSpawnerLogics;

        [SerializeField, Attach(Attach.Scene)] private CometsSpawnerLogics _cometsSpawnerLogics;
        [SerializeField, Attach(Attach.Scene)] private EnemySpawnerLogics _enemySpawnerLogics;

        [Header("Core")] [SerializeField, Attach(Attach.Scene)]
        private RestartGame _restartGame;

        public override void InstallBindings()
        {
            Install();
        }

        private async UniTaskVoid Install()
        {
            Container.Bind<ObjectPool<Point>>().FromInstance(_pointsPool).AsSingle();
            Container.Bind<ObjectPool<Comet>>().FromInstance(_cometsPool).AsSingle();
            Container.Bind<ObjectPool<Enemy>>().FromInstance(_enemyPool).AsSingle();
            Container.Bind<Spawner<Point>>().FromInstance(_pointsSpawner).AsSingle();
            Container.Bind<Spawner<Comet>>().FromInstance(_cometSpawner).AsSingle();
            Container.Bind<Spawner<Enemy>>().FromInstance(_enemySpawner).AsSingle();
            
            Container.Bind<CometsSpawnerLogics>().FromInstance(_cometsSpawnerLogics).AsSingle();
            Container.Bind<PointsSpawnerLogics>().FromInstance(_pointsSpawnerLogics).AsSingle();
            Container.Bind<EnemySpawnerLogics>().FromInstance(_enemySpawnerLogics).AsSingle();

            while (_playerMovement == null)
            {
                await UniTask.Yield();
            }
            
            Container.Bind<RestartGame>().FromInstance(_restartGame).AsSingle();

            Init();
        }

        private void Init()
        {
            _enemySpawner.OnObjectsInited += SendEnemyDependencies;
            
            _pointsSpawner.Init(_pointsPool);
            _cometSpawner.Init(_cometsPool);
            _enemySpawner.Init(_enemyPool);
        }

        private void OnDisable()
        {
            _enemySpawner.OnObjectsInited -= SendEnemyDependencies;
        }

        private void SendEnemyDependencies(List<Enemy> enemies)
        {
            foreach (var enemy in enemies)
            {
                enemy.InitDependencies(_cometsSpawnerLogics, _pointsSpawnerLogics);
            }
        }
    }
}