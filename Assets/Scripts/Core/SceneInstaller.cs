using Cinemachine;
using Dythervin.AutoAttach;
using PlanetIO;
using Pool;
using Spawner;
using UnityEngine;
using Zenject;

namespace PlanetIO_Core
{
    public class SceneInstaller : MonoInstaller
    {
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<Point> _pointsPool;
        [SerializeField, Attach(Attach.Scene)] private ObjectPool<Comet> _cometsPool;
        [SerializeField, Attach(Attach.Scene)] private Spawner<Point> _pointsSpawner;
        [SerializeField, Attach(Attach.Scene)] private Spawner<Comet> _cometSpawner;
        [SerializeField, Attach(Attach.Scene)] private PlayerMovement _playerMovement;
        [SerializeField, Attach(Attach.Scene)] private LogicsCometsSpawner  _logicsCometsSpawner;
        [SerializeField, Attach(Attach.Scene)] private CinemachineVirtualCamera _playerCamera;

        public override void InstallBindings()
        {
            Init();
            
            Container.Bind<ObjectPool<Point>>().FromInstance(_pointsPool).AsSingle();
            Container.Bind<ObjectPool<Comet>>().FromInstance(_cometsPool).AsSingle();
            Container.Bind<Spawner<Point>>().FromInstance(_pointsSpawner).AsSingle();
            Container.Bind<Spawner<Comet>>().FromInstance(_cometSpawner).AsSingle();
            Container.Bind<PlayerMovement>().FromInstance(_playerMovement).AsSingle();
            Container.Bind<LogicsCometsSpawner>().FromInstance(_logicsCometsSpawner).AsSingle();
            Container.Bind<CinemachineVirtualCamera>().FromInstance(_playerCamera).AsSingle();
        }

        private void Init()
        {
            _pointsSpawner.Init(_pointsPool);
            _cometSpawner.Init(_cometsPool);
        }
    }
}