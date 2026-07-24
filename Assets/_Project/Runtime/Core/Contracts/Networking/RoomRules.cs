using System;
using System.Linq;

namespace PlanetIO
{
    public static class RoomRules
    {
        public const int MinimumRoomCodeLength = 4;
        public const int MaximumRoomCodeLength = 12;
        public const int MinimumPlayers = 1;
        public const int MaximumPlayers = 16;
        public const int DefaultMaxPlayers = 4;
        public const string DefaultRoomCode = "PLANET";
        public const string ProtocolVersion = "planet-io/3";

        public static string NormalizeRoomCode(string roomCode)
        {
            string normalized = new(
                (roomCode ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .Take(MaximumRoomCodeLength)
                .ToArray());

            return normalized;
        }

        public static bool IsValidRoomCode(string roomCode)
        {
            string normalized = NormalizeRoomCode(roomCode);
            return normalized.Length >= MinimumRoomCodeLength && normalized.Length <= MaximumRoomCodeLength;
        }

        public static int ClampMaxPlayers(int maxPlayers) =>
            Math.Clamp(maxPlayers, MinimumPlayers, MaximumPlayers);

        public static bool TryCreateConnectionSettings(
            string roomCode,
            out RoomConnectionSettings settings,
            out string validationError)
        {
            string normalizedRoomCode = NormalizeRoomCode(roomCode);
            if (!IsValidRoomCode(normalizedRoomCode))
            {
                settings = default;
                validationError =
                    $"Room code must contain " +
                    $"{MinimumRoomCodeLength}–{MaximumRoomCodeLength} " +
                    "letters or digits.";
                return false;
            }

            settings = new RoomConnectionSettings(normalizedRoomCode, DefaultMaxPlayers);
            validationError = string.Empty;
            return true;
        }

        public static string CreateRoomCode() =>
            Guid.NewGuid()
                .ToString("N")
                [..6]
                .ToUpperInvariant();
    }
}
