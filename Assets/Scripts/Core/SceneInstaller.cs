using Cinemachine;
using Dythervin.AutoAttach;
using PlanetIO;
using UnityEngine;
using Zenject;

namespace PlanetIO_Core
{
    public class SceneInstaller : MonoInstaller
    {
        [SerializeField, Attach(Attach.Scene)] private PointsPool pointsObjectPool;
        [SerializeField, Attach(Attach.Scene)] private PointsSpawner pointsSpawner;
        [SerializeField, Attach(Attach.Scene)] private CometsPool cometsObjectPool;
        [SerializeField, Attach(Attach.Scene)] private CometsSpawner cometsSpawner;
        [SerializeField, Attach(Attach.Scene)] private CinemachineVirtualCamera _playerCamera;

        public override void InstallBindings()
        {
            Container.Bind<PointsPool>().FromInstance(pointsObjectPool).AsSingle();
            Container.Bind<CometsPool>().FromInstance(cometsObjectPool).AsSingle();
            Container.Bind<PointsSpawner>().FromInstance(pointsSpawner).AsSingle();
            Container.Bind<CometsSpawner>().FromInstance(cometsSpawner).AsSingle();
            Container.Bind<CinemachineVirtualCamera>().FromInstance(_playerCamera).AsSingle();
        }
    }
}