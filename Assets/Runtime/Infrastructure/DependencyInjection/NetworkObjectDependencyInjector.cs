using Unity.Netcode;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.DependencyInjection
{
    public sealed class NetworkObjectDependencyInjector : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            GameLifetimeScope scope = GameLifetimeScope.Instance;
            if (scope != null && scope.Container != null)
            {
                scope.Container.InjectGameObject(gameObject);
            }
        }
    }
}
