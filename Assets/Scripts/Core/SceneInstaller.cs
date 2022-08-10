using Cinemachine;
using Dythervin.AutoAttach;
using PlanetIO;
using UnityEngine;
using Zenject;

namespace PlanetIO_Core
{
    public class SceneInstaller : MonoInstaller
    {
        [SerializeField, Attach(Attach.Scene)] private PointsPool _pointsPool;
        [SerializeField, Attach(Attach.Scene)] private PointsGenerator _pointsGenerator;
        [SerializeField, Attach(Attach.Scene)] private CinemachineVirtualCamera _playerCamera;

        public override void InstallBindings()
        {
            Container.Bind<PointsPool>().FromInstance(_pointsPool).AsSingle();
            Container.Bind<PointsGenerator>().FromInstance(_pointsGenerator).AsSingle();
            Container.Bind<CinemachineVirtualCamera>().FromInstance(_playerCamera).AsSingle();
        }
    }
}