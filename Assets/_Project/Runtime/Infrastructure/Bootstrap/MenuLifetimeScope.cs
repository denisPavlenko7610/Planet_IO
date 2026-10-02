using VContainer;
using VContainer.Unity;
using PlanetIO.UI.Menu;
using PlanetIO.UI.Settings;

namespace PlanetIO.Infrastructure.Bootstrap
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>()
                .As<IMainMenuView>();

            builder.RegisterComponentInHierarchy<NicknameInputView>()
                .As<INicknameInputView>();

            builder.RegisterComponentInHierarchy<SkinSelectorView>()
                .As<ISkinSelectorView>();

            builder.RegisterEntryPoint<SkinSelectionPresenter>();

            builder.RegisterComponentInHierarchy<SettingsView>()
                .As<ISettingsView>();

            builder.RegisterEntryPoint<SettingsPresenter>().AsSelf();
            builder.RegisterEntryPoint<MenuPresenter>();
        }
    }
}
