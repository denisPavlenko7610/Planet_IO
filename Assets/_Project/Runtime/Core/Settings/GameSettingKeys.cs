using UnityEngine;
using UnityTemplates.Settings;

namespace PlanetIO
{
    public static class GameSettingKeys
    {
        public const string StorePrefix = "PlanetIO.Settings.";
        private const float DefaultVolume = 0.8f;

        public static readonly SettingKey<float> MusicVolume =
            new("audio.music-volume", DefaultVolume, SettingCodecs.Single, Mathf.Clamp01);

        public static readonly SettingKey<float> SfxVolume =
            new("audio.sfx-volume", DefaultVolume, SettingCodecs.Single, Mathf.Clamp01);

        public static readonly SettingKey<bool> HapticsEnabled =
            new("feedback.haptics", true, SettingCodecs.Boolean);

        public static readonly SettingKey<bool> TutorialCompleted =
            new("onboarding.tutorial-completed", false, SettingCodecs.Boolean);
    }
}
