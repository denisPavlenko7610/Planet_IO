using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using RD.Settings;

namespace PlanetIO
{
    [NoAutoStaticsCleanup]
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

        public static readonly SettingKey<bool> ControlsSwapped =
            new("gameplay.controls-swapped", false, SettingCodecs.Boolean);

        public static readonly SettingKey<bool> FixedJoystick =
            new("gameplay.fixed-joystick", false, SettingCodecs.Boolean);

        public static readonly SettingKey<int> BotDifficulty =
            new("gameplay.bot-difficulty", (int)PlanetIO.BotDifficulty.Medium,
                SettingCodecs.Int32, BotDifficultyRules.Normalize);

        public static readonly SettingKey<string> RoomCode =
            new("network.room-code", RoomRules.DefaultRoomCode, SettingCodecs.String, RoomRules.NormalizeRoomCode);

        public static readonly SettingKey<bool> TutorialCompleted =
            new("onboarding.tutorial-completed", false, SettingCodecs.Boolean);
    }
}
