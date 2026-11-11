using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PlanetIO.Editor.Localization;
using PlanetIO.UI;
using PlanetIO.UI.Hud;
using PlanetIO.UI.Menu;
using PlanetIO.UI.Settings;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace PlanetIO.Tests
{
    public sealed class LocalizationAndUiTests
    {
        private static readonly string[] RequiredLocales =
        {
            "en", "ru", "zh-Hans", "ja", "ko", "de", "fr", "es", "pt-BR", "it", "pl", "uk", "be", "tr"
        };

        private static readonly Regex Placeholder = new(@"\{\d+\}");

        [Test]
        public void Csv_ContainsExactlyTheCodeKeys()
        {
            List<string[]> rows = UIStringsImporter.ReadCsv(File.ReadAllText(UIStringsImporter.CsvPath, Encoding.UTF8));
            string[] csvKeys = rows.Skip(1).Select(row => row[0]).ToArray();

            Assert.That(csvKeys, Is.EquivalentTo(LocalizationKeys.All));
            Assert.That(rows[0].Skip(1), Is.EquivalentTo(RequiredLocales));
        }

        [Test]
        public void Fonts_CoverEveryCharacterOfEveryLocale()
        {
            const string fontFolder = "Assets/_Project/Art/Fonts/";
            Font latin = AssetDatabase.LoadAssetAtPath<Font>(fontFolder + "Nunito-Bold.ttf");
            Assert.That(latin, Is.Not.Null);

            Dictionary<string, string> cjkFonts = new()
            {
                ["zh-Hans"] = "NotoSansSC-Bold.ttf",
                ["ja"] = "NotoSansJP-Bold.ttf",
                ["ko"] = "NotoSansKR-Bold.ttf"
            };

            List<string[]> rows = UIStringsImporter.ReadCsv(File.ReadAllText(UIStringsImporter.CsvPath, Encoding.UTF8));
            for (int column = 1; column < rows[0].Length; column++)
            {
                string localeCode = rows[0][column];
                Font font = latin;
                if (cjkFonts.TryGetValue(localeCode, out string fontFile))
                {
                    font = AssetDatabase.LoadAssetAtPath<Font>(fontFolder + fontFile);
                    Assert.That(font, Is.Not.Null, fontFile);
                }

                foreach (string[] row in rows.Skip(1))
                {
                    foreach (char character in row[column])
                    {
                        if (!char.IsWhiteSpace(character) && !char.IsSurrogate(character))
                        {
                            Assert.That(font.HasCharacter(character), Is.True,
                                $"{localeCode}: '{character}' (U+{(int)character:X4}) in {row[0]} is missing from {font.name}");
                        }
                    }
                }
            }
        }

        [Test]
        public void StringTable_HasEveryKeyTranslatedForEveryLocale()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.TableName);
            Assert.That(collection, Is.Not.Null, "Run Planet IO/Localization/Import UI strings.");

            foreach (string localeCode in RequiredLocales)
            {
                StringTable table = (StringTable)collection.GetTable(localeCode);
                Assert.That(table, Is.Not.Null, $"Missing table for {localeCode}");

                foreach (string key in LocalizationKeys.All)
                {
                    StringTableEntry entry = table.GetEntry(key);
                    Assert.That(entry?.Value, Is.Not.Null.And.Not.Empty, $"{localeCode}: {key}");
                }
            }
        }

        [Test]
        public void Translations_KeepEnglishPlaceholders()
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.TableName);
            StringTable english = (StringTable)collection.GetTable("en");

            foreach (string localeCode in RequiredLocales)
            {
                StringTable table = (StringTable)collection.GetTable(localeCode);
                foreach (string key in LocalizationKeys.All)
                {
                    string[] expected = Placeholders(english.GetEntry(key).Value);
                    string[] actual = Placeholders(table.GetEntry(key).Value);
                    Assert.That(actual, Is.EquivalentTo(expected), $"{localeCode}: {key}");
                }
            }
        }

        [TestCase("Assets/_Project/UI/Menu.uxml", new[] { "play-with-bots", "quick-play", "create-room", "join-room", "room-code-input", "settings-button", "status-text", "best-score", "nickname-input", "random-nickname", "swatches", "skins", "settings-overlay" })]
        [TestCase("Assets/_Project/UI/Settings.uxml", new[] { "settings-overlay", "settings-backdrop", "music-slider", "sound-slider", "haptics-toggle", "previous-language", "next-language", "language-value", "ad-privacy", "privacy-policy", "settings-close" })]
        [TestCase("Assets/_Project/UI/Hud.uxml", new[] { "leave-button", "settings-button", "play-again", "continue-button", "defeat-leave", "defeat-panel", "session-text", "leaderboard-text", "score-text", "minimap", "stick", "boost-button", "border-warning", "loading-overlay" })]
        [TestCase("Assets/_Project/UI/Loading.uxml", new[] { "progress-fill", "loading-status" })]
        public void UiDocument_ContainsAllElementsRequiredByViews(string uxmlPath, string[] elementNames)
        {
            UnityEngine.UIElements.VisualTreeAsset asset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(uxmlPath);
            Assert.That(asset, Is.Not.Null, uxmlPath);

            UnityEngine.UIElements.VisualElement root = asset.Instantiate();
            foreach (string elementName in elementNames)
            {
                Assert.That(root.Q(elementName), Is.Not.Null, $"{uxmlPath}: {elementName}");
            }
        }

        [TestCase(0, 3, 1, 1)]
        [TestCase(2, 3, 1, 0)]
        [TestCase(0, 3, -1, 2)]
        [TestCase(-1, 3, 1, 1)]
        [TestCase(0, 0, 1, -1)]
        public void Settings_LanguageStepWrapsAround(int current, int count, int step, int expected)
        {
            Assert.That(SettingsPresenter.GetNextIndex(current, count, step), Is.EqualTo(expected));
        }

        [TestCase("ru", "Русский")]
        [TestCase("de", "Deutsch")]
        [TestCase("", "")]
        public void LanguageNames_UseNativeCapitalizedName(string code, string expected)
        {
            Assert.That(LanguageNames.GetNativeName(code), Is.EqualTo(expected));
        }

        private static string[] Placeholders(string value) =>
            Placeholder.Matches(value).Select(match => match.Value).Distinct().ToArray();
    }
}
