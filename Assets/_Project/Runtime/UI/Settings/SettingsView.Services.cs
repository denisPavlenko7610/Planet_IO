using System;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Settings
{
    public readonly struct ServicesViewState
    {
        public readonly bool PlayGamesAvailable;
        public readonly bool SignedIn;

        public ServicesViewState(
            bool playGamesAvailable,
            bool signedIn)
        {
            PlayGamesAvailable = playGamesAvailable;
            SignedIn = signedIn;
        }
    }

    public interface IServicesView
    {
        event Action SignInRequested;
        event Action LeaderboardsRequested;
        event Action AchievementsRequested;

        void ShowServices(ServicesViewState state);
    }

    public sealed partial class SettingsView : IServicesView
    {
        private VisualElement _servicesBlock;
        private Button _signInButton;
        private Button _leaderboardsButton;
        private Button _achievementsButton;
        private ServicesViewState _servicesState;
        private bool _hasServicesState;

        public event Action SignInRequested;
        public event Action LeaderboardsRequested;
        public event Action AchievementsRequested;

        public void ShowServices(ServicesViewState state)
        {
            _servicesState = state;
            _hasServicesState = true;
            if (_servicesBlock == null)
            {
                return;
            }

            SetVisible(_signInButton, state.PlayGamesAvailable && !state.SignedIn);
            SetVisible(_leaderboardsButton, state.PlayGamesAvailable && state.SignedIn);
            SetVisible(_achievementsButton, state.PlayGamesAvailable && state.SignedIn);

            _servicesBlock.style.display = state.PlayGamesAvailable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void EnableServices(VisualElement root)
        {
            _servicesBlock = root.Q<VisualElement>("services-block");
            _signInButton = root.Q<Button>("play-games-signin");
            _leaderboardsButton = root.Q<Button>("leaderboards-button");
            _achievementsButton = root.Q<Button>("achievements-button");

            _servicesBlock.style.display = DisplayStyle.None;
            _signInButton.clicked += OnSignIn;
            _leaderboardsButton.clicked += OnLeaderboards;
            _achievementsButton.clicked += OnAchievements;

            _labels.Bind(_signInButton, LocalizationKeys.SettingsPlayGamesSignIn);
            _labels.Bind(_leaderboardsButton, LocalizationKeys.SettingsLeaderboards);
            _labels.Bind(_achievementsButton, LocalizationKeys.SettingsAchievements);
            if (_hasServicesState)
            {
                ShowServices(_servicesState);
            }
        }

        private void DisableServices()
        {
            _signInButton.clicked -= OnSignIn;
            _leaderboardsButton.clicked -= OnLeaderboards;
            _achievementsButton.clicked -= OnAchievements;
        }

        private static void SetVisible(VisualElement element, bool visible) =>
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        private void OnSignIn() => SignInRequested?.Invoke();

        private void OnLeaderboards() => LeaderboardsRequested?.Invoke();

        private void OnAchievements() => AchievementsRequested?.Invoke();

    }
}
