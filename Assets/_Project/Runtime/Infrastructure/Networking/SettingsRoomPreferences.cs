using UnityEngine;
using RD.Settings;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class SettingsRoomPreferences : IRoomPreferences
    {
        private const string RoomCodeKey = "PlanetIO.Room.Code";

        private readonly ISettingsService _settings;

        public SettingsRoomPreferences(ISettingsService settings, ISettingsStore store)
        {
            _settings = settings;
            if (!store.TryRead(GameSettingKeys.RoomCode.Id, out _) && PlayerPrefs.HasKey(RoomCodeKey))
            {
                _settings.Set(GameSettingKeys.RoomCode, PlayerPrefs.GetString(RoomCodeKey));
                _settings.Flush();
            }
        }

        public RoomConnectionSettings Load() => new(
            _settings.Get(GameSettingKeys.RoomCode), RoomRules.DefaultMaxPlayers);

        public void Save(RoomConnectionSettings settings)
        {
            _settings.Set(GameSettingKeys.RoomCode, settings.RoomCode);
            _settings.Flush();
        }
    }
}
