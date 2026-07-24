using System;

namespace PlanetIO
{
    [Serializable]
    public readonly struct RoomConnectionSettings : IEquatable<RoomConnectionSettings>
    {
        public RoomConnectionSettings(string roomCode, int maxPlayers)
        {
            RoomCode = RoomRules.NormalizeRoomCode(roomCode);
            MaxPlayers = RoomRules.ClampMaxPlayers(maxPlayers);
        }

        public string RoomCode { get; }
        public int MaxPlayers { get; }

        public static RoomConnectionSettings Default =>
            new(RoomRules.DefaultRoomCode, RoomRules.DefaultMaxPlayers);

        public bool Equals(RoomConnectionSettings other)
        {
            return string.Equals(RoomCode, other.RoomCode, StringComparison.Ordinal) &&
                   MaxPlayers == other.MaxPlayers;
        }

        public override bool Equals(object obj) =>
            obj is RoomConnectionSettings other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(RoomCode, MaxPlayers);

        public override string ToString() =>
            $"{RoomCode} ({MaxPlayers})";
    }
}
