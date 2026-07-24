using System;
using System.Linq;

namespace Planet_IO
{
    public static class RoomRules
    {
        public const int MinimumRoomCodeLength = 4;
        public const int MaximumRoomCodeLength = 12;
        public const int MinimumPlayers = 1;
        public const int MaximumPlayers = 16;
        public const int DefaultMaxPlayers = 8;
        public const ushort DefaultPort = 7777;
        public const string DefaultAddress = "127.0.0.1";
        public const string DefaultRoomCode = "PLANET";
        public const string ProtocolVersion = "planet-io/2";

        public static string NormalizeRoomCode(string roomCode)
        {
            string normalized = new(
                (roomCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Where(character => char.IsLetterOrDigit(character))
                .Take(MaximumRoomCodeLength)
                .ToArray());

            return normalized;
        }

        public static bool IsValidRoomCode(string roomCode)
        {
            string normalized = NormalizeRoomCode(roomCode);
            return normalized.Length >= MinimumRoomCodeLength &&
                   normalized.Length <= MaximumRoomCodeLength;
        }

        public static string NormalizeAddress(string address)
        {
            string normalized = address?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(normalized)
                ? DefaultAddress
                : normalized;
        }

        public static int ClampMaxPlayers(int maxPlayers) =>
            Math.Clamp(maxPlayers, MinimumPlayers, MaximumPlayers);

        public static bool TryParsePort(string value, out ushort port)
        {
            if (ushort.TryParse(value?.Trim(), out port) && port > 0)
            {
                return true;
            }

            port = DefaultPort;
            return false;
        }
    }
}
