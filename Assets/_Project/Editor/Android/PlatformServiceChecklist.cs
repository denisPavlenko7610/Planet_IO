#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Editor
{
	public static class PlatformServiceChecklist
	{
		private const string PlayGamesSettingsPath = "Assets/GooglePlayGames/Resources/PlayGamesSettings.asset";
		private const string PlayGamesManifestPath =
			"Assets/Plugins/Android/GooglePlayGamesManifest.androidlib/AndroidManifest.xml";

		public static void Show()
		{
			List<string> missing = GetMissing();
			if (missing.Count == 0)
			{
				Debug.Log("Planet IO platform services: every ID is filled in.");
				return;
			}

			Debug.LogWarning(
				"Planet IO platform services are not fully configured:\n- " + string.Join("\n- ", missing)
			);
		}

		public static List<string> GetMissing()
		{
			var missing = new List<string>();
			if (!HasPlayGamesAppId())
			{
				missing.Add(
					"Play Games app ID: run Window > Google Play Games > Setup > Android setup and paste the resources from Play Console."
				);
			}

			foreach (LeaderboardKind kind in Enum.GetValues(typeof(LeaderboardKind)))
			{
				if (string.IsNullOrEmpty(PlatformServiceIds.LeaderboardId(kind)))
				{
					missing.Add($"Leaderboard ID for {kind} in PlatformServiceIds.");
				}
			}

			foreach (AchievementKind kind in Enum.GetValues(typeof(AchievementKind)))
			{
				if (string.IsNullOrEmpty(PlatformServiceIds.AchievementId(kind)))
				{
					missing.Add($"Achievement ID for {kind} in PlatformServiceIds.");
				}
			}

			return missing;
		}

		public static bool HasPlayGamesAppId()
		{
			if (!File.Exists(PlayGamesSettingsPath) || !File.Exists(PlayGamesManifestPath))
			{
				return false;
			}

			foreach (string line in File.ReadAllLines(PlayGamesSettingsPath))
			{
				string trimmed = line.Trim();
				if (trimmed.StartsWith("mAppId:", StringComparison.Ordinal))
				{
					return trimmed.Substring("mAppId:".Length).Trim().Length > 0;
				}
			}

			return false;
		}
	}
}
#endif