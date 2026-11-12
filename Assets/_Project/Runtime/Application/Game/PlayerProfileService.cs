using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using RD.Save;
using RD.Foundation;
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

        private const string ProfileSlot = "player-profile";
        private const int ProfileVersion = 1;
        private readonly SaveStore _saves;
        private readonly AsyncGate _saveGate = new();
        private bool _initialized;
        private bool _saving;
        private int _revision;
        private int _savedRevision;
        private Exception _saveError;
        private readonly Random _random = new();
        private readonly PlanetSkinCatalog _skinCatalog;
        private readonly HashSet<string> _unlockedSkins = new();

        public PlayerProfileService(PlanetSkinCatalog skinCatalog, SaveStore saves)
        {
            _skinCatalog = skinCatalog ? skinCatalog : throw new ArgumentNullException(nameof(skinCatalog));
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            SelectedSkin = FirstFreeSkin();
            Nickname = PlanetNames.Get(0);
            PreferredColor = PlayerPalette.GetColor(0);
        }

        public async Awaitable InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized) return;
            ProfileData data;
            try
            {
                data = await _saves.LoadAsync<ProfileData>(ProfileSlot, Validate, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                data = ImportLegacy();
                await _saves.SaveAsync(ProfileSlot, data, cancellationToken);
            }

            foreach (string id in data.UnlockedSkins) _unlockedSkins.Add(id);
            int savedSkin = _skinCatalog.IndexOf(data.SelectedSkin);
            SelectedSkin = savedSkin >= 0 && IsSkinUnlocked(savedSkin) ? savedSkin : FirstFreeSkin();
            Nickname = NicknameRules.Normalize(data.Nickname);
            BestScore = data.BestScore;
            PreferredColor = PlayerPalette.GetColor(data.ColorIndex);
            _initialized = true;
            NicknameChanged?.Invoke(Nickname);
            PreferredColorChanged?.Invoke(PreferredColor);
            SkinChanged?.Invoke(SelectedSkin);
            SkinsUnlocked?.Invoke();
        }

        private ProfileData ImportLegacy()
        {
            string nickname = PlayerPrefs.GetString(NicknameKey, string.Empty);
            return new ProfileData
            {
                Version = ProfileVersion,
                Nickname = string.IsNullOrWhiteSpace(nickname)
                    ? PlanetNames.Pick(_random) : NicknameRules.Normalize(nickname),
                ColorIndex = Mathf.Clamp(PlayerPrefs.GetInt(ColorIndexKey, 0), 0, PlayerPalette.Colors.Count - 1),
                BestScore = Mathf.Max(0, PlayerPrefs.GetInt(BestScoreKey, 0)),
                SelectedSkin = PlayerPrefs.GetString(SelectedSkinKey, string.Empty),
                UnlockedSkins = PlayerPrefs.GetString(UnlockedSkinsKey, string.Empty)
                    .Split(UnlockedSeparator).Where(id => !string.IsNullOrWhiteSpace(id)).ToArray()
            };
        }

        private static void Validate(ProfileData data)
        {
            if (data == null || data.Version != ProfileVersion || data.BestScore < 0 ||
                data.ColorIndex < 0 || data.ColorIndex >= PlayerPalette.Colors.Count ||
                string.IsNullOrWhiteSpace(data.Nickname) || data.UnlockedSkins == null ||
                data.UnlockedSkins.Any(string.IsNullOrWhiteSpace))
                throw new InvalidSaveDataException("Unsupported or invalid Planet_IO profile.");
        }

        private void Save()
        {
            if (!_initialized) throw new InvalidOperationException("Player profile has not been initialized.");
            _revision++;
            if (!_saving) _ = SavePendingAsync();
        }

        private async Awaitable SavePendingAsync()
        {
            _saving = true;
            using (await _saveGate.EnterAsync())
            {
                _saveError = null;
                try
                {
                    while (_savedRevision != _revision)
                    {
                        int revision = _revision;
                        ProfileData snapshot = new()
                        {
                            Version = ProfileVersion,
                            Nickname = Nickname,
                            ColorIndex = Mathf.Max(0, PlayerPalette.IndexOf(PreferredColor)),
                            BestScore = BestScore,
                            SelectedSkin = _skinCatalog.Get(SelectedSkin).Id,
                            UnlockedSkins = _unlockedSkins.OrderBy(id => id).ToArray()
                        };
                        await _saves.SaveAsync(ProfileSlot, snapshot);
                        _savedRevision = revision;
                    }
                }
                catch (Exception exception)
                {
                    _saveError = exception;
                    UnityLogger.LogException(exception);
                }
                finally { _saving = false; }
            }
        }

        public async Awaitable FlushAsync()
        {
            if (!_saving && _revision != _savedRevision) _ = SavePendingAsync();
            using (await _saveGate.EnterAsync())
            {
                if (_saveError != null) throw new IOException("Could not save player profile.", _saveError);
            }
        }

        [Serializable]
        public sealed class ProfileData
        {
            public int Version;
            public string Nickname;
            public int ColorIndex;
            public int BestScore;
            public string SelectedSkin;
            public string[] UnlockedSkins;
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
            Save();
            SkinsUnlocked?.Invoke();
        }

        public bool SelectSkin(int skinIndex)
        {
            if (!IsSkinUnlocked(skinIndex) || skinIndex == SelectedSkin)
            {
                return false;
            }

            SelectedSkin = skinIndex;
            Save();
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
            Save();
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
            Save();
            NicknameChanged?.Invoke(Nickname);
        }

        public void SetRandomNickname()
        {
            SetNickname(PlanetNames.Pick(_random, Nickname));
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
                Save();
            }

            PreferredColorChanged?.Invoke(color);
        }
    }
}
