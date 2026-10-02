using System;
using UnityEngine;

namespace PlanetIO
{
    public interface IPlayerProfileService
    {
        event Action<string> NicknameChanged;
        event Action<Color32> PreferredColorChanged;
        event Action<int> SkinChanged;
        event Action SkinsUnlocked;

        string Nickname { get; }
        Color32 PreferredColor { get; }
        int BestScore { get; }
        int SelectedSkin { get; }

        void SetNickname(string nickname);
        void SetRandomNickname();
        void SetPreferredColor(Color32 color);
        bool SubmitScore(int score);
        bool IsSkinUnlocked(int skinIndex);
        void UnlockSkin(int skinIndex);
        bool SelectSkin(int skinIndex);
    }
}
