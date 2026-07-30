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
            builder.RegisterComponentInHierarchy<LoadingView>()
                .As<ILoadingView>();

            builder.RegisterEntryPoint<LoadingPresenter>();
            builder.RegisterEntryPoint<LoadingSceneController>();
        }
    }
}
