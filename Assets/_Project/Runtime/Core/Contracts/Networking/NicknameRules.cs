using System.Linq;

namespace PlanetIO
{
    public static class NicknameRules
    {
        public const int MaximumLength = 20;
        public const string DefaultNickname = "Player";

        public static string Normalize(string nickname)
        {
            string normalizedNickname = new string(
                (nickname ?? string.Empty)
                .Where(character => !char.IsControl(character))
                .ToArray())
                .Trim();

            while (normalizedNickname.Contains("  "))
            {
                normalizedNickname = normalizedNickname.Replace("  ", " ");
            }

            if (normalizedNickname.Length > MaximumLength)
            {
                normalizedNickname =
                    normalizedNickname[..MaximumLength].TrimEnd();
            }

            return string.IsNullOrWhiteSpace(normalizedNickname)
                ? DefaultNickname
                : normalizedNickname;
        }
    }
}
