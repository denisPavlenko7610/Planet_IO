using System;
using UnityEngine;

namespace PlanetIO
{
    public interface IPlayerProfileService
    {
        event Action<string> NicknameChanged;
        event Action<Color32> PreferredColorChanged;

        string Nickname { get; }
        Color32 PreferredColor { get; }
        int BestScore { get; }

        void SetNickname(string nickname);
        void SetRandomNickname();
        void SetPreferredColor(Color32 color);
        bool SubmitScore(int score);
    }
}
