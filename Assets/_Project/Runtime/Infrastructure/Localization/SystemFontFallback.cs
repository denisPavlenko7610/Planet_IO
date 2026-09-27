using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace PlanetIO.Infrastructure.Localization
{
    public static class SystemFontFallback
    {
        private static readonly string[] CjkLanguagePrefixes = { "zh", "ja", "ko" };

        private static readonly string[] CjkFamilies =
        {
            "Noto Sans CJK SC", "Noto Sans CJK JP", "Noto Sans CJK KR", "Noto Sans SC", "Noto Sans JP",
            "Source Han Sans", "Droid Sans Fallback", "Microsoft YaHei", "Yu Gothic", "Malgun Gothic",
            "PingFang SC", "Hiragino Sans"
        };

        private static readonly string[] CjkFileHints = { "notosanscjk", "notosanssc", "droidsansfallback", "msyh", "yugoth", "malgun" };

        private static bool _cjkAttempted;

        public static void EnsureFor(string languageCode)
        {
            if (_cjkAttempted || !NeedsCjk(languageCode))
            {
                return;
            }

            _cjkAttempted = true;
            TMP_FontAsset fallback = CreateFromFamilies() ?? CreateFromFiles();
            if (fallback == null)
            {
                GameLogger.LogWarning("No CJK system font found; Chinese, Japanese and Korean text may not render.");
                return;
            }

            List<TMP_FontAsset> fallbacks = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();
            if (!fallbacks.Contains(fallback))
            {
                fallbacks.Add(fallback);
            }

            TMP_Settings.fallbackFontAssets = fallbacks;
        }

        private static bool NeedsCjk(string languageCode)
        {
            if (string.IsNullOrEmpty(languageCode))
            {
                return false;
            }

            foreach (string prefix in CjkLanguagePrefixes)
            {
                if (languageCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static TMP_FontAsset CreateFromFamilies()
        {
            foreach (string family in CjkFamilies)
            {
                try
                {
                    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(family, "Regular");
                    if (asset != null)
                    {
                        return asset;
                    }
                }
                catch (Exception exception)
                {
                    GameLogger.LogWarning($"System font '{family}' failed: {exception.Message}");
                }
            }

            return null;
        }

        private static TMP_FontAsset CreateFromFiles()
        {
            foreach (string path in Font.GetPathsToOSFonts())
            {
                string fileName = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                foreach (string hint in CjkFileHints)
                {
                    if (!fileName.Contains(hint))
                    {
                        continue;
                    }

                    TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                        path, 0, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                    if (asset != null)
                    {
                        return asset;
                    }
                }
            }

            return null;
        }
    }
}
