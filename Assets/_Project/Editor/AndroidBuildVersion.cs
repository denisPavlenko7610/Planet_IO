#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Editor
{
    public static class AndroidBuildVersion
    {
        public const string LastBuiltPreferenceKey = "PlanetIO.Android.LastBuiltVersionCode";

        public static int Resolve(int current, int lastBuilt)
        {
            return current > lastBuilt ? current : lastBuilt + 1;
        }

        public static int PrepareForBuild()
        {
            int current = PlayerSettings.Android.bundleVersionCode;
            int resolved = Resolve(current, EditorPrefs.GetInt(LastBuiltPreferenceKey, 0));
            if (resolved != current)
            {
                PlayerSettings.Android.bundleVersionCode = resolved;
                AssetDatabase.SaveAssets();
            }

            return resolved;
        }

        public static void RecordBuilt(int versionCode)
        {
            EditorPrefs.SetInt(LastBuiltPreferenceKey, versionCode);
        }

        [MenuItem("Planet IO/Android/Bump version code")]
        public static void Bump()
        {
            PlayerSettings.Android.bundleVersionCode++;
            AssetDatabase.SaveAssets();
            Debug.Log($"Android version code is now {PlayerSettings.Android.bundleVersionCode}.");
        }
    }
}
#endif
