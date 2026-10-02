using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace PlanetIO.Application
{
    public sealed class PlayerProfileService : IPlayerProfileService
    {
        private const string ColorIndexKey = "PlanetIO.ColorIndex";
        private const string NicknameKey = "PlanetIO.Nickname";
        private const string BestScoreKey = "PlanetIO.BestScore";
        private const string SelectedSkinKey = "PlanetIO.Skins.Selected";
        private const string UnlockedSkinsKey = "PlanetIO.Skins.Unlocked";
        private const char UnlockedSeparator = ',';

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
        private readonly PlanetSkinCatalog _skinCatalog;
        private readonly HashSet<string> _unlockedSkins = new();

        public PlayerProfileService(PlanetSkinCatalog skinCatalog)
        {
            _skinCatalog = skinCatalog ? skinCatalog : throw new ArgumentNullException(nameof(skinCatalog));
            foreach (string id in PlayerPrefs.GetString(UnlockedSkinsKey, string.Empty).Split(UnlockedSeparator))
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _unlockedSkins.Add(id);
                }
            }

            int savedSkin = _skinCatalog.IndexOf(PlayerPrefs.GetString(SelectedSkinKey, string.Empty));
            SelectedSkin = savedSkin >= 0 && IsSkinUnlocked(savedSkin) ? savedSkin : FirstFreeSkin();

            string savedNickname = PlayerPrefs.GetString(NicknameKey, string.Empty);
            if (string.IsNullOrWhiteSpace(savedNickname))
            {
                SetRandomNickname();
            }
            else
            {
                SetNickname(savedNickname);
            }

            BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
            PreferredColor = PlayerPalette.GetColor(
                PlayerPrefs.GetInt(ColorIndexKey, 0));
        }

        public event Action<string> NicknameChanged;
        public event Action<Color32> PreferredColorChanged;

        public string Nickname { get; private set; }
        public Color32 PreferredColor { get; private set; }
        public int BestScore { get; private set; }
        public int SelectedSkin { get; private set; }

        public event Action<int> SkinChanged;
        public event Action SkinsUnlocked;

        public bool IsSkinUnlocked(int skinIndex)
        {
            if (!_skinCatalog.IsValid(skinIndex))
            {
                return false;
            }

            PlanetSkinCatalog.Skin skin = _skinCatalog.Get(skinIndex);
            return skin.Free || _unlockedSkins.Contains(skin.Id);
        }

        public void UnlockSkin(int skinIndex)
        {
            if (!_skinCatalog.IsValid(skinIndex) || IsSkinUnlocked(skinIndex))
            {
                return;
            }

            _unlockedSkins.Add(_skinCatalog.Get(skinIndex).Id);
            PlayerPrefs.SetString(UnlockedSkinsKey, string.Join(UnlockedSeparator, _unlockedSkins));
            PlayerPrefs.Save();
            SkinsUnlocked?.Invoke();
        }

        public bool SelectSkin(int skinIndex)
        {
            if (!IsSkinUnlocked(skinIndex) || skinIndex == SelectedSkin)
            {
                return false;
            }

            SelectedSkin = skinIndex;
            PlayerPrefs.SetString(SelectedSkinKey, _skinCatalog.Get(skinIndex).Id);
            PlayerPrefs.Save();
            SkinChanged?.Invoke(skinIndex);
            return true;
        }

        private int FirstFreeSkin()
        {
            for (int index = 0; index < _skinCatalog.Count; index++)
            {
                if (_skinCatalog.Get(index).Free)
                {
                    return index;
                }
            }

            return 0;
        }

        public bool SubmitScore(int score)
        {
            if (score <= BestScore)
            {
                return false;
            }

            BestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, score);
            PlayerPrefs.Save();
            return true;
        }

        public void SetNickname(string nickname)
        {
            string normalizedNickname = NicknameRules.Normalize(nickname);
            if (Nickname == normalizedNickname)
            {
                return;
            }

            Nickname = normalizedNickname;
            PlayerPrefs.SetString(NicknameKey, normalizedNickname);
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
