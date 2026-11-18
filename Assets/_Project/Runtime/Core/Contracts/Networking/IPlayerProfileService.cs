using System;

namespace PlanetIO
{
    public interface IPlayerProfileService
    {
        event Action<string> NicknameChanged;
        event Action<int> SkinChanged;
        event Action SkinsUnlocked;

        string Nickname { get; }
        int BestScore { get; }
        int SelectedSkin { get; }

        void SetNickname(string nickname);
        void SetRandomNickname();
        bool SubmitScore(int score);
        bool IsSkinUnlocked(int skinIndex);
        void UnlockSkin(int skinIndex);
        bool SelectSkin(int skinIndex);
    }
}
