using System;
using VContainer.Unity;

namespace PlanetIO.UI.Menu
{
    public sealed class PlayGamesMenuPresenter : IStartable, IDisposable
    {
        private readonly IPlayGamesMenuView _view;
        private readonly IPlayGamesService _playGames;

        public PlayGamesMenuPresenter(IPlayGamesMenuView view, IPlayGamesService playGames)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _playGames = playGames ?? throw new ArgumentNullException(nameof(playGames));
        }

        public void Start()
        {
            _view.LeaderboardsRequested += OnLeaderboards;
            _view.AchievementsRequested += OnAchievements;
            _playGames.StateChanged += Render;
            Render();
        }

        public void Dispose()
        {
            _view.LeaderboardsRequested -= OnLeaderboards;
            _view.AchievementsRequested -= OnAchievements;
            _playGames.StateChanged -= Render;
        }

        private void Render() => _view.SetPlayGamesVisible(_playGames.IsAvailable && _playGames.IsSignedIn);

        private void OnLeaderboards() => _playGames.ShowLeaderboards();

        private void OnAchievements() => _playGames.ShowAchievements();
    }
}
