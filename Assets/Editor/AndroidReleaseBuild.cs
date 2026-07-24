#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PlanetIO.Editor
{
    public static class AndroidReleaseBuild
    {
        private const string ApplicationIdentifier = "com.rd.planetio";
        private const string BuildDirectory = "Builds/Android";
        private const string BundleName = "PlanetIO.aab";

        [MenuItem("Planet IO/Android/Apply release settings")]
        public static void ApplyReleaseSettings()
        {
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android,
                ApplicationIdentifier);
            PlayerSettings.Android.minSdkVersion =
                AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion =
                AndroidSdkVersions.AndroidApiLevel36;
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures =
                AndroidArchitecture.ARMv7 |
                AndroidArchitecture.ARM64;
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android,
                ManagedStrippingLevel.Low);

            PlayerSettings.defaultInterfaceOrientation =
                UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.optimizedFramePacing = true;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            EditorUserBuildSettings.buildAppBundle = true;

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Planet IO Android settings applied: API 36, " +
                "IL2CPP, ARMv7 + ARM64, AAB.");
        }

        [MenuItem("Planet IO/Android/Validate release")]
        public static void ValidateRelease()
        {
            string error = GetValidationError();
            if (!string.IsNullOrEmpty(error))
            {
                throw new BuildFailedException(error);
            }

            Debug.Log("Planet IO is configured for an Android release build.");
        }

        [MenuItem("Planet IO/Android/Build release AAB")]
        public static void BuildReleaseBundle()
        {
            ApplyReleaseSettings();
            ValidateRelease();

            Directory.CreateDirectory(BuildDirectory);
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName =
                    Path.Combine(BuildDirectory, BundleName),
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.CleanBuildCache
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"Android build failed: {report.summary.result}");
            }

            Debug.Log(
                $"Android App Bundle created: " +
                $"{options.locationPathName} " +
                $"({report.summary.totalSize / 1048576f:0.0} MB).");
        }

        private static string GetValidationError()
        {
            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                return "Android Build Support is not installed for this Unity Editor.";
            }

            if (PlayerSettings.Android.targetSdkVersion !=
                AndroidSdkVersions.AndroidApiLevel36)
            {
                return "Target API must be Android 16 / API 36.";
            }

            if ((PlayerSettings.Android.targetArchitectures &
                 AndroidArchitecture.ARM64) == 0)
            {
                return "ARM64 architecture is required.";
            }

            if (PlayerSettings.GetScriptingBackend(
                    NamedBuildTarget.Android) !=
                ScriptingImplementation.IL2CPP)
            {
                return "Android release must use IL2CPP.";
            }

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                return "Configure a private Android keystore before publishing.";
            }

            if (!EditorBuildSettings.scenes.Any(scene => scene.enabled))
            {
                return "No enabled scenes are configured for the build.";
            }

            return string.Empty;
        }
    }
}
#endif
