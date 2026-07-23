namespace Planet_IO
{
    public static class NicknameRules
    {
        public const int MaximumLength = 20;
        public const string DefaultNickname = "Player";

        public static string Normalize(string nickname)
        {
            string normalizedNickname = nickname?.Trim() ?? string.Empty;
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
