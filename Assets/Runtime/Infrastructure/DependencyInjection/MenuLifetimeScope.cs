using VContainer;
using VContainer.Unity;
using PlanetIO.UI.Menu;

namespace PlanetIO.Infrastructure
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<NetworkUI>();
        }

        protected override LifetimeScope FindParent()
        {
            return ApplicationLifetimeScope.Instance;
        }
    }
}
