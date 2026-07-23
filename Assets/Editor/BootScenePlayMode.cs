using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlanetIO.Editor
{
    [InitializeOnLoad]
    internal static class BootScenePlayMode
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";

        static BootScenePlayMode()
        {
            EditorApplication.delayCall += Configure;
        }

        [MenuItem("Planet IO/Configure Boot Play Mode")]
        private static void Configure()
        {
            SceneAsset bootScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootScene != null && EditorSceneManager.playModeStartScene != bootScene)
            {
                EditorSceneManager.playModeStartScene = bootScene;
            }
        }
    }
}
