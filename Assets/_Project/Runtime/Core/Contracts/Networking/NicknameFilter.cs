using System.Text;

namespace PlanetIO
{
    public static class NicknameFilter
    {
        private static readonly string[] BlockedFragments =
        {
            "fuck", "shit", "bitch", "cunt", "nigger", "nigga", "faggot", "whore", "slut", "dick", "pussy", "rape",
            "nazi", "hitler",
            "хуй", "хуе", "хуё", "пизд", "ебат", "ебан", "еблан", "бляд", "сука", "мудак", "пидор", "пидар", "залуп", "шлюх", "гандон"
        };

        public static bool IsAllowed(string nickname)
        {
            string compact = Compact(nickname);
            foreach (string fragment in BlockedFragments)
            {
                if (compact.Contains(fragment))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Compact(string value)
        {
            StringBuilder builder = new();
            foreach (char character in value ?? string.Empty)
            {
                char lower = char.ToLowerInvariant(character);
                builder.Append(lower switch
                {
                    '0' => 'o',
                    '1' => 'i',
                    '3' => 'e',
                    '4' => 'a',
                    '5' => 's',
                    '@' => 'a',
                    '$' => 's',
                    _ => lower
                });
            }

            StringBuilder letters = new();
            foreach (char character in builder.ToString())
            {
                if (char.IsLetter(character))
                {
                    letters.Append(character);
                }
            }

            return letters.ToString();
        }
    }
}
