#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
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

		public static void ApplyReleaseSettings()
		{
			PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationIdentifier);
			PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
			PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
			PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
			PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
			PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);

			PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
			PlayerSettings.allowedAutorotateToPortrait = false;
			PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
			PlayerSettings.allowedAutorotateToLandscapeLeft = true;
			PlayerSettings.allowedAutorotateToLandscapeRight = true;
			PlayerSettings.Android.optimizedFramePacing = true;
			PlayerSettings.Android.renderOutsideSafeArea = true;
			PlayerSettings.Android.requestedVisibleInsets = AndroidWindowInsetsType.None;
			PlayerSettings.Android.requestedOverlappingInsets =
				AndroidWindowInsetsType.StatusBars | AndroidWindowInsetsType.NavigationBars;
			PlayerSettings.Android.systemBarsBackground = AndroidSystemBarsBackground.Transparent;
			PlayerSettings.Android.systemBarsBehavior = AndroidSystemBarsBehavior.ShowTransientBarsBySwipe;
			PlayerSettings.Android.minifyRelease = true;
			EditorUserBuildSettings.buildAppBundle = true;
			EditorUserBuildSettings.development = false;
			UserBuildSettings.DebugSymbols.level = Unity.Android.Types.DebugSymbolLevel.SymbolTable;

			AndroidAppIcons.Apply();
			AndroidSigning.ApplyFromEnvironment();

			AssetDatabase.SaveAssets();
			Debug.Log("Planet IO Android settings applied: API 36, IL2CPP, ARM64, AAB, release flags.");
		}

		public static void ValidateRelease()
		{
			string error = GetValidationError();
			if (!string.IsNullOrEmpty(error))
			{
				throw new BuildFailedException(error);
			}

			List<string> missingServices = PlatformServiceChecklist.GetMissing();
			if (missingServices.Count > 0)
			{
				Debug.LogWarning(
					"Planet IO platform services are not fully configured (the build is still allowed):\n- " +
					string.Join("\n- ", missingServices)
				);
			}

			Debug.Log("Planet IO is configured for an Android release build.");
		}

		[MenuItem("Planet IO/Android/Build release AAB")]
		public static void BuildReleaseBundle()
		{
			ApplyReleaseSettings();
			ValidateRelease();
			int versionCode = AndroidBuildVersion.PrepareForBuild();

			string buildDirectory =
				Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", BuildDirectory));
			if (Directory.Exists(buildDirectory))
			{
				Directory.Delete(buildDirectory, recursive: true);
			}

			Directory.CreateDirectory(buildDirectory);
			string[] scenes = EditorBuildSettings.scenes
				.Where(scene => scene.enabled)
				.Select(scene => scene.path)
				.ToArray();

			BuildPlayerOptions options = new()
			{
				scenes = scenes,
				locationPathName = Path.Combine(buildDirectory, BundleName),
				target = BuildTarget.Android,
				targetGroup = BuildTargetGroup.Android,
				options = BuildOptions.CleanBuildCache
			};

			BuildReport report = BuildPipeline.BuildPlayer(options);
			if (report.summary.result != BuildResult.Succeeded)
			{
				throw new BuildFailedException($"Android build failed: {report.summary.result}");
			}

			AndroidBuildVersion.RecordBuilt(versionCode);
			Debug.Log(
				$"Android App Bundle created: " +
				$"{options.locationPathName} " +
				$"({report.summary.totalSize / 1048576f:0.0} MB), " +
				$"version {PlayerSettings.bundleVersion} (code {versionCode})."
			);
		}

		private static string GetValidationError()
		{
			if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
			{
				return "Android Build Support is not installed for this Unity Editor.";
			}

			if (PlayerSettings.Android.targetSdkVersion != AndroidSdkVersions.AndroidApiLevel36)
			{
				return "Target API must be Android 16 / API 36.";
			}

			if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
			{
				return "ARM64 architecture is required.";
			}

			if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
			{
				return "Android release must use IL2CPP.";
			}

			if (EditorUserBuildSettings.development)
			{
				return "Development build is on: rewarded ads would use test ad unit IDs.";
			}

			if (!PlayerSettings.Android.minifyRelease)
			{
				return "Android release must enable Minify Release (R8).";
			}

			AndroidWindowInsetsType systemBars =
				AndroidWindowInsetsType.StatusBars | AndroidWindowInsetsType.NavigationBars;
			if (!PlayerSettings.Android.renderOutsideSafeArea ||
				(PlayerSettings.Android.requestedOverlappingInsets & systemBars) != systemBars ||
				PlayerSettings.Android.requestedVisibleInsets != AndroidWindowInsetsType.None ||
				PlayerSettings.Android.systemBarsBackground != AndroidSystemBarsBackground.Transparent ||
				PlayerSettings.Android.systemBarsBehavior != AndroidSystemBarsBehavior.ShowTransientBarsBySwipe)
			{
				return "Android release must use immersive edge-to-edge rendering. Apply release settings first.";
			}

			if (PlayerSettings.Android.bundleVersionCode < 1)
			{
				return "Android version code must be 1 or higher.";
			}

			if (!EditorBuildSettings.scenes.Any(scene => scene.enabled))
			{
				return "No enabled scenes are configured for the build.";
			}

			string iconError = AndroidAppIcons.GetValidationError();
			if (!string.IsNullOrEmpty(iconError))
			{
				return iconError;
			}

			return AndroidSigning.GetValidationError();
		}
	}

	internal sealed class AndroidBurstCacheCleanup : IPreprocessBuildWithReport
	{
		public int callbackOrder => int.MinValue;

		public void OnPreprocessBuild(BuildReport report)
		{
			if (report.summary.platform != BuildTarget.Android ||
				UnityEngine.Application.unityVersion != "7000.0.0a16")
			{
				return;
			}

			string projectDirectory = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
			string cacheDirectory = Path.Combine(projectDirectory, "Library", "BurstCache", "Android-Arm");
			if (Directory.Exists(cacheDirectory))
			{
				Directory.Delete(cacheDirectory, recursive: true);
			}

			string outputDirectory = Path.Combine(projectDirectory, "Temp", "BurstOutput");
			if (Directory.Exists(outputDirectory))
			{
				Directory.Delete(outputDirectory, recursive: true);
			}
			Debug.Log(
				"Cleared Android Burst AOT cache and generated plugin to prevent stale pointer-size specializations."
			);
		}
	}
}
#endif