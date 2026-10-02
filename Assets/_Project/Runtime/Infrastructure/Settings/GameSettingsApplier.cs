using System;
using UnityTemplates.Haptics;
using UnityTemplates.Settings;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Settings
{
    public sealed class GameSettingsApplier : IInitializable, IDisposable
    {
        private readonly ISettingsService _settings;

        public GameSettingsApplier(ISettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Initialize()
        {
            _settings.Changed += OnSettingChanged;
            Apply();
        }

        public void Dispose()
        {
            _settings.Changed -= OnSettingChanged;
            _settings.Flush();
        }

        private void OnSettingChanged(SettingChanged _)
        {
            Apply();
        }

        private void Apply()
        {
            Haptics.IsEnabled = _settings.Get(GameSettingKeys.HapticsEnabled);
        }
    }
}
