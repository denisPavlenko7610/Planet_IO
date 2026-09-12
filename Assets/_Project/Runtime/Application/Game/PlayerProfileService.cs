using System;
using UnityEngine;
using Random = System.Random;

namespace PlanetIO.Application
{
    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private const string ColorIndexKey = "PlanetIO.ColorIndex";

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
            PreferredColor = PlayerPalette.GetColor(
                PlayerPrefs.GetInt(ColorIndexKey, 0));
        }

        public event Action<string> NicknameChanged;
        public event Action<Color32> PreferredColorChanged;

        public string Nickname { get; private set; }
        public Color32 PreferredColor { get; private set; }

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

        public void SetPreferredColor(Color32 color)
        {
            if ((Color)PreferredColor == (Color)color)
            {
                return;
            }

            PreferredColor = color;
            int colorIndex = PlayerPalette.IndexOf(color);
            if (colorIndex >= 0)
            {
                PlayerPrefs.SetInt(ColorIndexKey, colorIndex);
            }

            PreferredColorChanged?.Invoke(color);
        }
    }
}
