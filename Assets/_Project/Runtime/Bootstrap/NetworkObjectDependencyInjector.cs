using System;
using Unity.Netcode;
using VContainer;
using VContainer.Unity;

namespace PlanetIO.Bootstrap
{
    public sealed class NetworkObjectDependencyInjector : NetworkBehaviour
    {
        private bool _injected;
        private IObjectResolver _container;

        [Inject]
        public void Construct(IObjectResolver container)
        {
            _container = container;
        }

        private void Awake()
        {
            InjectDependencies();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InjectDependencies();
        }

        private void InjectDependencies()
        {
            if (_injected)
            {
                return;
            }

            if (_container != null)
            {
                _container.InjectGameObject(gameObject);
                _injected = true;
            }
        }
    }
}
