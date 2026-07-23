using VContainer;
using VContainer.Unity;
using PlanetIO.Infrastructure.Loading;
using PlanetIO.UI.Loading;

namespace PlanetIO.Infrastructure
{
    public sealed class LoadingLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<LoadingView>();
            builder.RegisterEntryPoint<LoadingSceneController>();
        }

        protected override LifetimeScope FindParent()
        {
            return ApplicationLifetimeScope.Instance;
        }
    }
}
