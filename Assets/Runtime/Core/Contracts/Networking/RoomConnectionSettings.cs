using System;

namespace Planet_IO
{
    [Serializable]
    public readonly struct RoomConnectionSettings : IEquatable<RoomConnectionSettings>
    {
        public RoomConnectionSettings(
            string roomCode,
            string address,
            ushort port,
            int maxPlayers)
        {
            RoomCode = RoomRules.NormalizeRoomCode(roomCode);
            Address = RoomRules.NormalizeAddress(address);
            Port = port == 0 ? RoomRules.DefaultPort : port;
            MaxPlayers = RoomRules.ClampMaxPlayers(maxPlayers);
        }

        public string RoomCode { get; }
        public string Address { get; }
        public ushort Port { get; }
        public int MaxPlayers { get; }

        public static RoomConnectionSettings Default =>
            new(
                RoomRules.DefaultRoomCode,
                RoomRules.DefaultAddress,
                RoomRules.DefaultPort,
                RoomRules.DefaultMaxPlayers);

        public bool Equals(RoomConnectionSettings other)
        {
            return string.Equals(RoomCode, other.RoomCode, StringComparison.Ordinal) &&
                   string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase) &&
                   Port == other.Port &&
                   MaxPlayers == other.MaxPlayers;
        }

        public override bool Equals(object obj) =>
            obj is RoomConnectionSettings other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                RoomCode,
                Address?.ToUpperInvariant(),
                Port,
                MaxPlayers);

        public override string ToString() =>
            $"{RoomCode} @ {Address}:{Port} ({MaxPlayers})";
    }
}
