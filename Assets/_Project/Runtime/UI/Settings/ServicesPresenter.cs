using System;
using RD.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Settings
{
    public sealed class ServicesPresenter : IStartable, IDisposable
    {
        private readonly IServicesView _view;
        private readonly IPlayGamesService _playGames;
        private readonly ICloudSyncService _cloud;
        private readonly ILocalizationService _localization;

        public ServicesPresenter(
            IServicesView view,
            IPlayGamesService playGames,
            ICloudSyncService cloud,
            ILocalizationService localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _playGames = playGames ?? throw new ArgumentNullException(nameof(playGames));
            _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public void Start()
        {
            _view.SignInRequested += OnSignIn;
            _view.LeaderboardsRequested += OnLeaderboards;
            _view.AchievementsRequested += OnAchievements;
            _playGames.StateChanged += Render;
            _localization.LocaleChanged += OnLocaleChanged;
            Render();
        }

        public void Dispose()
        {
            _view.SignInRequested -= OnSignIn;
            _view.LeaderboardsRequested -= OnLeaderboards;
            _view.AchievementsRequested -= OnAchievements;
            _playGames.StateChanged -= Render;
            _localization.LocaleChanged -= OnLocaleChanged;
        }

        private void Render()
        {
            _view.ShowServices(new ServicesViewState(
                _playGames.IsAvailable,
                _playGames.IsSignedIn));
        }

        private void OnLocaleChanged(LocaleChanged _) => Render();

        private void OnSignIn() => _cloud.SignIn(true, _ => Render());

        private void OnLeaderboards() => _playGames.ShowLeaderboards();

        private void OnAchievements() => _playGames.ShowAchievements();

    }
}
