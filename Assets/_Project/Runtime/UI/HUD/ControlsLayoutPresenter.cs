using System;
using RD.Settings;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class ControlsLayoutPresenter : IStartable, IDisposable
    {
        private readonly ISessionHudView _view;
        private readonly ISettingsService _settings;

        public ControlsLayoutPresenter(ISessionHudView view, ISettingsService settings)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Start()
        {
            _settings.Changed += OnSettingChanged;
            Apply();
        }

        public void Dispose()
        {
            _settings.Changed -= OnSettingChanged;
        }

        private void OnSettingChanged(SettingChanged _) => Apply();

        private void Apply() => _view.SetControlsSwapped(_settings.Get(GameSettingKeys.ControlsSwapped));
    }
}
