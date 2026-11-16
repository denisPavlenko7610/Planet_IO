using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlanetIO.Bootstrap
{
    [DefaultExecutionOrder(-4000)]
    public sealed class NetworkObjectDependencyInjector : NetworkBehaviour
    {
        private bool _injected;
        private IObjectResolver _container;

        [Inject]
        public void Construct(IObjectResolver container)
        {
            _container = container;
            _injected = true;
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

            GameLifetimeScope scope = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();
            _container = scope != null ? scope.Container : null;
            if (_container == null)
            {
                throw new InvalidOperationException("Network objects require an initialized GameLifetimeScope.");
            }

            _injected = true;
            _container.InjectGameObject(gameObject);
        }
    }
}
