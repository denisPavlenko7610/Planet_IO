#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace PlanetIO.Editor.Localization
{
    public static class UIStringsImporter
    {
        public const string CsvPath = "Assets/_Project/Localization/UIStrings.csv";
        private const string LocalizationFolder = "Assets/_Project/Localization";
        private const string LocalesFolder = LocalizationFolder + "/Locales";
        private const string TablesFolder = LocalizationFolder + "/Tables";
        private const string SettingsPath = LocalizationFolder + "/LocalizationSettings.asset";
        private const string DefaultLocaleCode = "en";
        private const string PlayerPrefsLocaleKey = "PlanetIO.Locale";

        [MenuItem("Planet IO/Localization/Import UI strings")]
        public static void Import()
        {
            List<string[]> rows = ReadCsv(File.ReadAllText(CsvPath, Encoding.UTF8));
            string[] header = rows[0];

            EnsureFolder(LocalizationFolder);
            EnsureFolder(LocalesFolder);
            EnsureFolder(TablesFolder);

            LocalizationSettings settings = EnsureSettings();
            List<Locale> locales = new();
            for (int column = 1; column < header.Length; column++)
            {
                locales.Add(EnsureLocale(header[column].Trim()));
            }

            ConfigureStartupSelectors(settings);

            StringTableCollection collection =
                LocalizationEditorSettings.GetStringTableCollection(LocalizationKeys.TableName) ??
                LocalizationEditorSettings.CreateStringTableCollection(LocalizationKeys.TableName, TablesFolder, locales);

            foreach (Locale locale in locales)
            {
                if (collection.GetTable(locale.Identifier) == null)
                {
                    collection.AddNewTable(locale.Identifier);
                }
            }

            for (int rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                string[] row = rows[rowIndex];
                string key = row[0].Trim();
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                if (collection.SharedData.GetEntry(key) == null)
                {
                    collection.SharedData.AddKey(key);
                }

                for (int column = 1; column < header.Length && column < row.Length; column++)
                {
                    StringTable table = (StringTable)collection.GetTable(locales[column - 1].Identifier);
                    table.AddEntry(key, row[column].Replace("\\n", "\n"));
                    EditorUtility.SetDirty(table);
                }
            }

            HashSet<string> csvKeys = new(rows.Skip(1).Select(row => row[0].Trim()));
            foreach (string staleKey in collection.SharedData.Entries.Select(entry => entry.Key).Where(key => !csvKeys.Contains(key)).ToList())
            {
                collection.SharedData.RemoveKey(staleKey);
            }

            collection.SetPreloadTableFlag(true);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"Imported {rows.Count - 1} UI strings for {locales.Count} locales.");
        }

        private static LocalizationSettings EnsureSettings()
        {
            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null)
            {
                return settings;
            }

            settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            return settings;
        }

        private static Locale EnsureLocale(string code)
        {
            LocaleIdentifier identifier = new(code);
            Locale locale = LocalizationEditorSettings.GetLocale(identifier);
            if (locale != null)
            {
                return locale;
            }

            locale = Locale.CreateLocale(identifier);
            string displayName = identifier.CultureInfo != null ? identifier.CultureInfo.EnglishName : code;
            locale.name = displayName;
            AssetDatabase.CreateAsset(locale, $"{LocalesFolder}/{code}.asset");
            LocalizationEditorSettings.AddLocale(locale);
            return locale;
        }

        private static void ConfigureStartupSelectors(LocalizationSettings settings)
        {
            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new PlayerPrefLocaleSelector { PlayerPreferenceKey = PlayerPrefsLocaleKey });
            selectors.Add(new SystemLocaleSelector());
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(DefaultLocaleCode) });
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static List<string[]> ReadCsv(string text)
        {
            List<string[]> rows = new();
            List<string> fields = new();
            StringBuilder field = new();
            bool quoted = false;

            for (int index = 0; index < text.Length; index++)
            {
                char current = text[index];
                if (quoted)
                {
                    if (current == '"' && index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (current == '"')
                    {
                        quoted = false;
                    }
                    else
                    {
                        field.Append(current);
                    }

                    continue;
                }

                switch (current)
                {
                    case '"':
                        quoted = true;
                        break;
                    case ',':
                        fields.Add(field.ToString());
                        field.Clear();
                        break;
                    case '\r':
                        break;
                    case '\n':
                        fields.Add(field.ToString());
                        field.Clear();
                        rows.Add(fields.ToArray());
                        fields.Clear();
                        break;
                    default:
                        field.Append(current);
                        break;
                }
            }

            if (field.Length > 0 || fields.Count > 0)
            {
                fields.Add(field.ToString());
                rows.Add(fields.ToArray());
            }

            return rows.Where(row => row.Length > 1 || row[0].Length > 0).ToList();
        }
    }
}
#endif
