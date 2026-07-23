using System;

namespace Planet_IO
{
    public interface IPlayerProfileService
    {
        event Action<string> NicknameChanged;

        string Nickname { get; }

        void SetNickname(string nickname);
        void SetRandomNickname();
    }
}
