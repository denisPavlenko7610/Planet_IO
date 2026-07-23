using System;

namespace Planet_IO.Application
{
    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private static readonly string[] AvailableNicknames =
        {
            "Bob",
            "Tom",
            "Riki",
            "Rock",
            "Margaret",
            "Monika"
        };

        private readonly Random _random = new();

        public PlayerProfileService()
        {
            SetRandomNickname();
        }

        public event Action<string> NicknameChanged;

        public string Nickname { get; private set; }

        public void SetNickname(string nickname)
        {
            string normalizedNickname = NicknameRules.Normalize(nickname);
            if (Nickname == normalizedNickname)
            {
                return;
            }

            Nickname = normalizedNickname;
            NicknameChanged?.Invoke(Nickname);
        }

        public void SetRandomNickname()
        {
            SetNickname(AvailableNicknames[_random.Next(AvailableNicknames.Length)]);
        }
    }
}
