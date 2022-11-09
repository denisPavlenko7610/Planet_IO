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
            var guids = AssetDatabase.FindAssets("t:preset", new[] { "Assets" });

            foreach (string guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);
                var type = preset.GetPresetType();
                var list = new List<DefaultPreset>(Preset.GetDefaultPresetsForType(type));
                list.Add(new DefaultPreset(null, preset));
                Preset.SetDefaultPresetsForType(type, list.ToArray());
            }
        }
    }
}