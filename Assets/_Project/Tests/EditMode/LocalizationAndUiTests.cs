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

        [TestCase("Assets/_Project/Prefabs/UI/Menu.prefab", typeof(MainMenuView))]
        [TestCase("Assets/_Project/Prefabs/UI/Menu.prefab", typeof(SettingsView))]
        [TestCase("Assets/_Project/Prefabs/UI/Menu.prefab", typeof(NicknameInputView))]
        [TestCase("Assets/_Project/Prefabs/UI/SessionHud.prefab", typeof(SessionHudView))]
        public void UiPrefab_HasAllSerializedReferencesAssigned(string prefabPath, System.Type viewType)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);

            Component view = prefab.GetComponentInChildren(viewType, true);
            Assert.That(view, Is.Not.Null, viewType.Name);

            SerializedProperty property = new SerializedObject(view).GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
                {
                    Assert.That(property.objectReferenceValue, Is.Not.Null, $"{viewType.Name}.{property.name}");
                }
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
