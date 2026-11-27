using System;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Menu
{
    public interface IPlayGamesMenuView
    {
        event Action LeaderboardsRequested;
        event Action AchievementsRequested;

        void SetPlayGamesVisible(bool visible);
    }

    public sealed partial class MainMenuView : IPlayGamesMenuView
    {
        private Button _leaderboardsButton;
        private Button _achievementsButton;
        private bool _playGamesVisible;

        public event Action LeaderboardsRequested;
        public event Action AchievementsRequested;

        public void SetPlayGamesVisible(bool visible)
        {
            _playGamesVisible = visible;
            ApplyPlayGamesVisibility();
        }

        private void EnablePlayGamesView(VisualElement root)
        {
            _leaderboardsButton = root.Q<Button>("menu-leaderboards-button");
            _achievementsButton = root.Q<Button>("menu-achievements-button");
            _leaderboardsButton.clicked += OnLeaderboards;
            _achievementsButton.clicked += OnAchievements;
            ApplyPlayGamesVisibility();
        }

        private void DisablePlayGamesView()
        {
            _leaderboardsButton.clicked -= OnLeaderboards;
            _achievementsButton.clicked -= OnAchievements;
        }

        private void ApplyPlayGamesVisibility()
        {
            if (_leaderboardsButton == null)
            {
                return;
            }

            DisplayStyle style = _playGamesVisible ? DisplayStyle.Flex : DisplayStyle.None;
            _leaderboardsButton.style.display = style;
            _achievementsButton.style.display = style;
        }

        private void OnLeaderboards() => LeaderboardsRequested?.Invoke();

        private void OnAchievements() => AchievementsRequested?.Invoke();
    }
}
