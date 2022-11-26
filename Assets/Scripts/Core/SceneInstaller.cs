﻿using Cinemachine;
using Planet_IO;
using Planet_IO.ObjectPool;
using RDTools.AutoAttach;
using UnityEngine;
using Zenject;

namespace PlanetIO_Core
{
    public class SceneInstaller : MonoInstaller
    {
        [Header("Pool and Spawner")]
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<Point> _pointsPool;
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<EnemyScale> _enemyPool;
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<Comet> _cometsPool;
        [SerializeField, Attach(Attach.Scene)] private Spawner<Point> _pointsSpawner;
        [SerializeField, Attach(Attach.Scene)] private Spawner<Comet> _cometSpawner;
        [SerializeField, Attach(Attach.Scene)] private Spawner<EnemyScale> _enemySpawner;
        

        [Header("Player")]
        [SerializeField, Attach(Attach.Scene)] private PlayerMovement _playerMovement;
        [SerializeField, Attach(Attach.Scene)] private PlayerScale _playerScale;
        [SerializeField, Attach(Attach.Scene)] private CinemachineVirtualCamera _playerCamera;
        
        [Header("logics Spawner")] 
        [SerializeField, Attach(Attach.Scene)] private PointsSpawnerLogics _pointsSpawnerLogics;
        [SerializeField, Attach(Attach.Scene)] private CometsSpawnerLogics _cometsSpawnerLogics;
        [SerializeField, Attach(Attach.Scene)] private EnemySpawnerLogics _enemySpawnerLogics;
        
        [Header("Core")] 
        [SerializeField, Attach(Attach.Scene)] private RestartGame _restartGame;
        

        public override void InstallBindings()
        {
            Init();
            
            Container.Bind<ObjectPool<Point>>().FromInstance(_pointsPool).AsSingle();
            Container.Bind<ObjectPool<Comet>>().FromInstance(_cometsPool).AsSingle();
            Container.Bind<ObjectPool<EnemyScale>>().FromInstance(_enemyPool).AsSingle();
            Container.Bind<Spawner<Point>>().FromInstance(_pointsSpawner).AsSingle();
            Container.Bind<Spawner<Comet>>().FromInstance(_cometSpawner).AsSingle();
            Container.Bind<Spawner<EnemyScale>>().FromInstance(_enemySpawner).AsSingle();
            
            Container.Bind<CinemachineVirtualCamera>().FromInstance(_playerCamera).AsSingle();
            Container.Bind<PlayerMovement>().FromInstance(_playerMovement).AsSingle();
            Container.Bind<PlayerScale>().FromInstance(_playerScale).AsSingle();
            
            Container.Bind<CometsSpawnerLogics>().FromInstance(_cometsSpawnerLogics).AsSingle();
            Container.Bind<PointsSpawnerLogics>().FromInstance(_pointsSpawnerLogics).AsSingle();
            Container.Bind<EnemySpawnerLogics>().FromInstance(_enemySpawnerLogics).AsSingle();

            Container.Bind<RestartGame>().FromInstance(_restartGame).AsSingle();
        }

        private void Init()
        {
            _pointsSpawner.Init(_pointsPool);
            _cometSpawner.Init(_cometsPool);
            _enemySpawner.Init(_enemyPool);
        }
    }
}