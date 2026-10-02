using System;
using PlanetIO.UI.Settings;
using UnityEngine;
using UnityTemplates.Localization;
using UnityTemplates.Pause;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class HudNavigationPresenter : IStartable, IDisposable
    {
        private const string SettingsPauseReason = "settings";

        private readonly ISessionHudView _view;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ILocalizationService _localization;
        private readonly SettingsPresenter _settingsPresenter;
        private readonly IPauseService _pauseService;
        private IDisposable _settingsPause;
        private bool _leaveInProgress;

        public HudNavigationPresenter(
            ISessionHudView view,
            INetworkSessionService networkSessionService,
            ILocalizationService localization,
            SettingsPresenter settingsPresenter,
            IPauseService pauseService)
        {
            _pauseService = pauseService ?? throw new ArgumentNullException(nameof(pauseService));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settingsPresenter = settingsPresenter ?? throw new ArgumentNullException(nameof(settingsPresenter));
        }

        public void Start()
        {
            _view.LeaveRequested += OnLeaveRequested;
            _view.SettingsRequested += OnSettingsRequested;
            _settingsPresenter.Closed += OnSettingsClosed;
            _localization.LocaleChanged += OnLocaleChanged;
            RenderLabels();
        }

        public void Dispose()
        {
            _view.LeaveRequested -= OnLeaveRequested;
            _view.SettingsRequested -= OnSettingsRequested;
            _settingsPresenter.Closed -= OnSettingsClosed;
            _localization.LocaleChanged -= OnLocaleChanged;
            ReleasePause();
        }

        private void OnLocaleChanged(LocaleChanged _) => RenderLabels();

        private void RenderLabels()
        {
            _view.SetButtonLabels(
                _localization.Get(LocalizationKeys.HudLeave),
                _localization.Get(LocalizationKeys.HudPlayAgain),
                _localization.Get(LocalizationKeys.HudWatchAd));
        }

        private void OnSettingsRequested()
        {
            if (_networkSessionService.Mode == NetworkSessionMode.SinglePlayer)
            {
                _settingsPause ??= _pauseService.RequestPause(SettingsPauseReason);
            }

            _settingsPresenter.Open();
        }

        private void OnSettingsClosed() => ReleasePause();

        private void ReleasePause()
        {
            _settingsPause?.Dispose();
            _settingsPause = null;
        }

        private void OnLeaveRequested()
        {
            _ = LeaveAsync();
        }

        private async Awaitable LeaveAsync()
        {
            if (_leaveInProgress)
            {
                return;
            }

            _leaveInProgress = true;
            ReleasePause();
            _view.SetLeaveButtonInteractable(false);

            try
            {
                await _networkSessionService.ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                GameLogger.LogException(exception);
                _leaveInProgress = false;
                _view.SetLeaveButtonInteractable(true);
            }
        }
    }
}
