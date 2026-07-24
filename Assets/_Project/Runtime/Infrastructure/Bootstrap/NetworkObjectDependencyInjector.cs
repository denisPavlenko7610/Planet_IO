using Unity.Netcode;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.DependencyInjection
{
    public sealed class NetworkObjectDependencyInjector : NetworkBehaviour
    {
        private bool _injected;

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

            GameLifetimeScope scope = GameLifetimeScope.Instance;
            if (scope != null && scope.Container != null)
            {
                scope.Container.InjectGameObject(gameObject);
                _injected = true;
            }
        }
    }
}
