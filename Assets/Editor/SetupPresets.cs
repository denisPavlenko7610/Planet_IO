using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;

namespace Planet_IO
{
    public class SetupPresets
    {
        [MenuItem("Menu/SetupPresets")]
        static void Setup()
        {
            string[] guids = AssetDatabase.FindAssets("t:preset", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Preset preset = AssetDatabase.LoadAssetAtPath<Preset>(path);
                PresetType type = preset.GetPresetType();
                List<DefaultPreset> list = new List<DefaultPreset>(Preset.GetDefaultPresetsForType(type));
                list.Add(new DefaultPreset(null, preset));
                Preset.SetDefaultPresetsForType(type, list.ToArray());
            }
        }
    }
}