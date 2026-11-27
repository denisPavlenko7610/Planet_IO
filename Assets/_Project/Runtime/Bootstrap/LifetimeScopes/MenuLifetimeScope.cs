using VContainer;
using VContainer.Unity;
using PlanetIO.UI.Menu;
using PlanetIO.UI.Settings;

namespace PlanetIO.Bootstrap
{
    public sealed class MenuLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<MainMenuView>()
                .As<IMainMenuView>()
                .As<IProgressView>()
				.As<IPlayGamesMenuView>();

            builder.RegisterComponentInHierarchy<NicknameInputView>()
                .As<INicknameInputView>();

            builder.RegisterComponentInHierarchy<SkinSelectorView>()
                .As<ISkinSelectorView>();

            builder.RegisterEntryPoint<SkinSelectionPresenter>();

            builder.RegisterComponentInHierarchy<SettingsView>()
                .As<ISettingsView>()
                .As<IServicesView>();

            builder.RegisterEntryPoint<SettingsPresenter>().AsSelf();
            builder.RegisterEntryPoint<ServicesPresenter>();
            builder.RegisterEntryPoint<MenuPresenter>();
            builder.RegisterEntryPoint<ProgressMenuPresenter>();
            builder.RegisterEntryPoint<PlayGamesMenuPresenter>();
        }
    }
}
