using System;
using RD.Localization;
using RD.Settings;
using VContainer.Unity;

namespace PlanetIO.UI.Menu
{
    public sealed class ModeMenuPresenter : IStartable, IDisposable
    {
        private readonly IModeView _view;
        private readonly ISettingsService _settings;
        private readonly ILocalizationService _localization;

        public ModeMenuPresenter(IModeView view, ISettingsService settings, ILocalizationService localization)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public void Start()
        {
            _view.BlitzChanged += OnBlitzChanged;
            _localization.LocaleChanged += OnLocaleChanged;
            Render();
        }

        public void Dispose()
        {
            _view.BlitzChanged -= OnBlitzChanged;
            _localization.LocaleChanged -= OnLocaleChanged;
        }

        private void OnLocaleChanged(LocaleChanged _) => Render();

        private void Render()
        {
            _view.ShowBlitz(_settings.Get(GameSettingKeys.BlitzMode), _localization.Get(LocalizationKeys.ModeBlitz));
        }

        private void OnBlitzChanged(bool enabled)
        {
            _settings.Set(GameSettingKeys.BlitzMode, enabled);
            _settings.Flush();
        }
    }
}
