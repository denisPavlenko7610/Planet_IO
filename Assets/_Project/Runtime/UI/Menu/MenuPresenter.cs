using System;
using PlanetIO.UI.Settings;
using UnityEngine;
using VContainer.Unity;
using UnityTemplates.Localization;

namespace PlanetIO.UI.Menu
{
    public sealed class MenuPresenter : IStartable, IDisposable
    {
        private readonly IMainMenuView _menuView;
        private readonly INicknameInputView _nicknameInputView;
        private readonly INetworkSessionService _networkSessionService;
        private readonly IPlayerProfileService _playerProfileService;
        private readonly IRoomPreferences _roomPreferences;
        private readonly ILocalizationService _localization;
        private readonly SettingsPresenter _settingsPresenter;
        private bool _sessionRequestInProgress;
        private bool _showingLocalError;

        public MenuPresenter(
            IMainMenuView menuView,
            INicknameInputView nicknameInputView,
            INetworkSessionService networkSessionService,
            IPlayerProfileService playerProfileService,
            IRoomPreferences roomPreferences,
            ILocalizationService localization,
            SettingsPresenter settingsPresenter)
        {
            _menuView = menuView ?? throw new ArgumentNullException(nameof(menuView));
            _nicknameInputView = nicknameInputView ?? throw new ArgumentNullException(nameof(nicknameInputView));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
            _roomPreferences = roomPreferences ?? throw new ArgumentNullException(nameof(roomPreferences));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settingsPresenter = settingsPresenter ?? throw new ArgumentNullException(nameof(settingsPresenter));
        }

        public void Start()
        {
            _menuView.PlayWithBotsRequested += OnPlayWithBotsRequested;
            _menuView.CreateRoomRequested += OnCreateRoomRequested;
            _menuView.JoinRoomRequested += OnJoinRoomRequested;
            _menuView.SettingsRequested += OnSettingsRequested;
            _nicknameInputView.NicknameChanged += OnNicknameChanged;
            _nicknameInputView.RandomNicknameRequested += OnRandomNicknameRequested;
            _nicknameInputView.ColorSelected += OnColorSelected;
            _playerProfileService.NicknameChanged += OnProfileNicknameChanged;
            _playerProfileService.PreferredColorChanged += OnProfileColorChanged;
            _networkSessionService.StateChanged += OnSessionStateChanged;
            _localization.LocaleChanged += OnLocaleChanged;

            _nicknameInputView.ShowNickname(_playerProfileService.Nickname);
            _nicknameInputView.ShowSelectedColor(_playerProfileService.PreferredColor);
            _menuView.ShowRoomCode(LoadLastRoomCode());
            _menuView.SetInteractionEnabled(true);
            RenderTexts();
        }

        public void Dispose()
        {
            _menuView.PlayWithBotsRequested -= OnPlayWithBotsRequested;
            _menuView.CreateRoomRequested -= OnCreateRoomRequested;
            _menuView.JoinRoomRequested -= OnJoinRoomRequested;
            _menuView.SettingsRequested -= OnSettingsRequested;
            _nicknameInputView.NicknameChanged -= OnNicknameChanged;
            _nicknameInputView.RandomNicknameRequested -= OnRandomNicknameRequested;
            _nicknameInputView.ColorSelected -= OnColorSelected;
            _playerProfileService.NicknameChanged -= OnProfileNicknameChanged;
            _playerProfileService.PreferredColorChanged -= OnProfileColorChanged;
            _networkSessionService.StateChanged -= OnSessionStateChanged;
            _localization.LocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(LocaleChanged _)
        {
            RenderTexts();
        }

        private void RenderTexts()
        {
            int bestScore = _playerProfileService.BestScore;
            _menuView.ShowBestScore(bestScore > 0 ? _localization.Get(LocalizationKeys.MenuBestScore, bestScore) : string.Empty);

            if (!_showingLocalError)
            {
                RenderSessionStatus();
            }
        }

        private void RenderSessionStatus()
        {
            NetworkSessionState state = _networkSessionService.State;
            _menuView.ShowStatus(
                SessionStatusFormatter.Format(_localization, state, _networkSessionService.Mode,
                    _networkSessionService.CurrentRoom.RoomCode, _networkSessionService.LastFailure),
                SessionStatusFormatter.IsError(state));
        }

        private void OnPlayWithBotsRequested()
        {
            _ = StartSessionAsync(_networkSessionService.StartSinglePlayerAsync);
        }

        private void OnCreateRoomRequested()
        {
            _ = StartSessionAsync(() => _networkSessionService.StartHostAsync(RoomRules.DefaultMaxPlayers));
        }

        private void OnJoinRoomRequested(string roomCode)
        {
            string normalized = RoomRules.NormalizeRoomCode(roomCode);
            if (!RoomRules.IsValidRoomCode(normalized))
            {
                _showingLocalError = true;
                _menuView.ShowStatus(_localization.Get(LocalizationKeys.StatusEnterRoomCode), true);
                return;
            }

            _roomPreferences.Save(new RoomConnectionSettings(normalized, RoomRules.DefaultMaxPlayers));
            _ = StartSessionAsync(() => _networkSessionService.StartClientAsync(normalized));
        }

        private void OnSettingsRequested()
        {
            _settingsPresenter.Open();
        }

        private void OnNicknameChanged(string nickname)
        {
            _playerProfileService.SetNickname(nickname);
        }

        private void OnRandomNicknameRequested()
        {
            _playerProfileService.SetRandomNickname();
        }

        private void OnColorSelected(Color32 color)
        {
            _playerProfileService.SetPreferredColor(color);
        }

        private void OnProfileColorChanged(Color32 color)
        {
            _nicknameInputView.ShowSelectedColor(color);
        }

        private void OnProfileNicknameChanged(string nickname)
        {
            _nicknameInputView.ShowNickname(nickname);
        }

        private void OnSessionStateChanged(NetworkSessionState state, string status)
        {
            _showingLocalError = false;
            RenderSessionStatus();

            if (state == NetworkSessionState.Failed)
            {
                GameLogger.LogWarning($"Session failed: {status}");
                RestoreInteraction();
            }
        }

        private string LoadLastRoomCode()
        {
            string roomCode = _roomPreferences.Load().RoomCode;
            return roomCode == RoomRules.DefaultRoomCode ? string.Empty : roomCode;
        }

        private async Awaitable StartSessionAsync(Func<Awaitable<bool>> startSession)
        {
            if (_sessionRequestInProgress)
            {
                return;
            }

            _sessionRequestInProgress = true;
            _showingLocalError = false;
            _menuView.SetInteractionEnabled(false);

            try
            {
                if (await startSession())
                {
                    return;
                }

                RestoreInteraction();
                GameLogger.LogWarning($"Session start failed: {_networkSessionService.Status}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                RestoreInteraction();
                GameLogger.LogException(exception);
            }
        }

        private void RestoreInteraction()
        {
            _sessionRequestInProgress = false;
            _menuView.SetInteractionEnabled(true);
        }
    }
}
