using System;
using System.Collections.Generic;
using System.Linq;
using UnityTemplates.Localization;
using UnityTemplates.Settings;
using VContainer.Unity;

namespace PlanetIO.UI.Settings
{
    public sealed class SettingsPresenter : IStartable, IDisposable
    {
        private readonly ISettingsView _view;
        private readonly ISettingsService _settings;
        private readonly ILocalizationService _localization;
        private readonly IAdPrivacyService _adPrivacyService;

        public SettingsPresenter(
            ISettingsView view,
            ISettingsService settings,
            ILocalizationService localization,
            IAdPrivacyService adPrivacyService)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _adPrivacyService = adPrivacyService ?? throw new ArgumentNullException(nameof(adPrivacyService));
        }

        public bool IsOpen => _view.IsVisible;

        public event Action Opened;
        public event Action Closed;

        public void Start()
        {
            _view.MusicVolumeChanged += OnMusicVolumeChanged;
            _view.SfxVolumeChanged += OnSfxVolumeChanged;
            _view.HapticsChanged += OnHapticsChanged;
            _view.PreviousLanguageRequested += OnPreviousLanguage;
            _view.NextLanguageRequested += OnNextLanguage;
            _view.AdPrivacyRequested += OnAdPrivacyRequested;
            _view.CloseRequested += Close;
            _localization.LocaleChanged += OnLocaleChanged;
            _adPrivacyService.PrivacyOptionsChanged += Render;
            Render();
        }

        public void Dispose()
        {
            _view.MusicVolumeChanged -= OnMusicVolumeChanged;
            _view.SfxVolumeChanged -= OnSfxVolumeChanged;
            _view.HapticsChanged -= OnHapticsChanged;
            _view.PreviousLanguageRequested -= OnPreviousLanguage;
            _view.NextLanguageRequested -= OnNextLanguage;
            _view.AdPrivacyRequested -= OnAdPrivacyRequested;
            _view.CloseRequested -= Close;
            _localization.LocaleChanged -= OnLocaleChanged;
            _adPrivacyService.PrivacyOptionsChanged -= Render;
        }

        public void Open()
        {
            Render();
            _view.Show();
            Opened?.Invoke();
        }

        public void Close()
        {
            _settings.Flush();
            _view.Hide();
            Closed?.Invoke();
        }

        public static int GetNextIndex(int current, int count, int step)
        {
            if (count <= 0)
            {
                return -1;
            }

            int start = current < 0 ? 0 : current;
            return ((start + step) % count + count) % count;
        }

        private void Render()
        {
            _view.ShowValues(
                _settings.Get(GameSettingKeys.MusicVolume),
                _settings.Get(GameSettingKeys.SfxVolume),
                _settings.Get(GameSettingKeys.HapticsEnabled));
            _view.ShowLanguage(LanguageNames.GetNativeName(_localization.CurrentLocale));
            _view.SetAdPrivacyVisible(_adPrivacyService.IsPrivacyOptionsRequired);
        }

        private void OnLocaleChanged(LocaleChanged _) => Render();

        private void OnMusicVolumeChanged(float value) => _settings.Set(GameSettingKeys.MusicVolume, value);

        private void OnSfxVolumeChanged(float value) => _settings.Set(GameSettingKeys.SfxVolume, value);

        private void OnHapticsChanged(bool enabled) => _settings.Set(GameSettingKeys.HapticsEnabled, enabled);

        private void OnPreviousLanguage() => _ = StepLanguageAsync(-1);

        private void OnNextLanguage() => _ = StepLanguageAsync(1);

        private async UnityEngine.Awaitable StepLanguageAsync(int step)
        {
            if (_localization.IsChangingLocale)
            {
                return;
            }

            List<string> locales = _localization.AvailableLocales.ToList();
            int next = GetNextIndex(locales.IndexOf(_localization.CurrentLocale), locales.Count, step);
            if (next < 0)
            {
                return;
            }

            try
            {
                await _localization.SetLocaleAsync(locales[next]);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                GameLogger.LogException(exception);
            }
        }

        private void OnAdPrivacyRequested() => _adPrivacyService.ShowPrivacyOptions();
    }
}
