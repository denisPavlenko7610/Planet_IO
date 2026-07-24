using Planet_IO;
using UnityEngine;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class PlayerPrefsRoomPreferences : IRoomPreferences
    {
        private const string RoomCodeKey = "PlanetIO.Room.Code";

        public RoomConnectionSettings Load()
        {
            return new RoomConnectionSettings(
                PlayerPrefs.GetString(
                    RoomCodeKey,
                    RoomRules.DefaultRoomCode),
                RoomRules.DefaultMaxPlayers);
        }

        public void Save(RoomConnectionSettings settings)
        {
            PlayerPrefs.SetString(RoomCodeKey, settings.RoomCode);
            PlayerPrefs.Save();
        }
    }
}
