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
        [SerializeField, Attach(Attach.Scene)] private PointsPool _pointsObjectPool;
        [SerializeField, Attach(Attach.Scene)] private CometsPool _cometsObjectPool;
        [SerializeField, Attach(Attach.Scene)] private PointsSpawner _pointsSpawner;
        [SerializeField, Attach(Attach.Scene)] private CometsSpawner _cometSpawner;
        [SerializeField, Attach(Attach.Scene)] private CinemachineVirtualCamera _playerCamera;

        public override void InstallBindings()
        {
            Container.Bind<IPool<Point>>().FromInstance(_pointsObjectPool);
            Container.Bind<IPool<Comet>>().FromInstance(_cometsObjectPool).AsSingle();
            Container.Bind<ISpawner<Point>>().FromInstance(_pointsSpawner).AsSingle();
            Container.Bind<ISpawner<Comet>>().FromInstance(_cometSpawner).AsSingle();
            Container.Bind<CinemachineVirtualCamera>().FromInstance(_playerCamera).AsSingle();
            
            Init();
        }

        private void Init()
        {
            _pointsSpawner.Init(_pointsObjectPool);
            _cometSpawner.Init(_cometsObjectPool);
        }
    }
}