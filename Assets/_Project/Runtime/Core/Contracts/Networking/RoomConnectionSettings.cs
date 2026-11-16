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
            Difficulty = BotDifficulty.Medium;
        }

        public RoomConnectionSettings(string roomCode, int maxPlayers, BotDifficulty difficulty) : this(roomCode, maxPlayers)
        {
            Difficulty = (BotDifficulty)BotDifficultyRules.Normalize((int)difficulty);
        }

        public string RoomCode { get; }
        public int MaxPlayers { get; }
        public BotDifficulty Difficulty { get; }

        public static RoomConnectionSettings Default => new(RoomRules.DefaultRoomCode, RoomRules.DefaultMaxPlayers);

        public bool Equals(RoomConnectionSettings other)
        {
            return string.Equals(RoomCode, other.RoomCode, StringComparison.Ordinal) && MaxPlayers == other.MaxPlayers && Difficulty == other.Difficulty;
        }

        public override bool Equals(object obj) => obj is RoomConnectionSettings other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(RoomCode, MaxPlayers, Difficulty);

        public override string ToString() => $"{RoomCode} ({MaxPlayers})";
    }
}
