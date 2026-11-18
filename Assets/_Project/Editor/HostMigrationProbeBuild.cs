using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace PlanetIO.Editor
{
	[InitializeOnLoad]
	public static class HostMigrationProbeBuild
	{
		private const string Pending = "PlanetIO.MigrationProbe.Pending";
		private const string Previous = "PlanetIO.MigrationProbe.Defines";
		private const string Define = "PLANET_IO_MIGRATION_PROBE";

		static HostMigrationProbeBuild()
		{
			EditorApplication.update += Check;
		}

		public static void Build()
		{
			if (SessionState.GetBool(Pending, false))
			{
				throw new InvalidOperationException("Migration probe build is already pending.");
			}

			string original = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
			SessionState.SetString(Previous, original);
			SessionState.SetBool(Pending, true);
			PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, original + ";" + Define);
		}

		private static void Check()
		{
			if (!SessionState.GetBool(Pending, false) ||
				EditorApplication.isCompiling ||
				EditorApplication.isUpdating ||
				BuildPipeline.isBuildingPlayer)
			{
				return;
			}
			if (!PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone).Contains(Define))
			{
				return;
			}

			SessionState.SetBool(Pending, false);
			try
			{
				Directory.CreateDirectory("Temp/MigrationPeerBuild");
				var report = BuildPipeline.BuildPlayer(
					new BuildPlayerOptions
					{
						target = BuildTarget.StandaloneWindows64,
						locationPathName = "Temp/MigrationPeerBuild/PlanetIO.exe",
						options = BuildOptions.Development,
						scenes = new[]
						{
							"Assets/_Project/Scenes/Boot.unity", "Assets/_Project/Scenes/MainMenu.unity",
							"Assets/_Project/Scenes/Loading.unity", "Assets/_Project/Scenes/Game.unity"
						}
					}
				);
				File.WriteAllText(
					"Temp/MigrationPeerBuild/result.txt",
					report.summary.result + ";errors=" + report.summary.totalErrors
				);
			}
			catch (Exception error)
			{
				File.WriteAllText("Temp/MigrationPeerBuild/result.txt", error.ToString());
				Debug.LogException(error);
			}
			finally
			{
				PlayerSettings.SetScriptingDefineSymbols(
					NamedBuildTarget.Standalone,
					SessionState.GetString(Previous, string.Empty)
				);
			}
		}
	}
}