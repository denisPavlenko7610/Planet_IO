using System;
using System.Linq;
using UnityEngine;

namespace PlanetIO
{
    [Serializable]
    public sealed class CloudSaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public ProgressionState Progression = new();
        public string Nickname = string.Empty;
        public string SelectedSkin = string.Empty;
        public string[] UnlockedSkins = Array.Empty<string>();
        public int BestScore;
        public int TotalKills;
		public int RankedTotalKills;
		public int RankedBestScore;
        public string[] Achievements = Array.Empty<string>();

        public bool IsValid() =>
            Version == CurrentVersion &&
            Progression != null &&
            Progression.IsValid() &&
            !string.IsNullOrWhiteSpace(Nickname) &&
            BestScore >= 0 &&
            TotalKills >= 0 &&
			RankedTotalKills >= 0 &&
			RankedBestScore >= 0 &&
			RankedBestScore <= ScoreValidationRules.MaxScore &&
            UnlockedSkins != null &&
            Achievements != null;

        public CloudSaveData Clone() => JsonUtility.FromJson<CloudSaveData>(JsonUtility.ToJson(this));

        public string ToJson() => JsonUtility.ToJson(this);
    }

    public readonly struct CloudMergeResult
    {
        public readonly CloudSaveData Data;
        public readonly bool ChangesLocal;
        public readonly bool ChangesCloud;

        public CloudMergeResult(CloudSaveData data, bool changesLocal, bool changesCloud)
        {
            Data = data;
            ChangesLocal = changesLocal;
            ChangesCloud = changesCloud;
        }
    }

    public static class CloudSaveRules
    {
        public static CloudMergeResult Merge(CloudSaveData local, CloudSaveData cloud)
        {
            bool localValid = local != null && local.IsValid();
            bool cloudValid = cloud != null && cloud.IsValid();
            if (!localValid && !cloudValid)
            {
                return new CloudMergeResult(null, false, false);
            }

            if (!cloudValid)
            {
                return new CloudMergeResult(Normalize(local), false, true);
            }

            if (!localValid)
            {
                return new CloudMergeResult(Normalize(cloud), true, false);
            }

            CloudSaveData winner = CloudWins(local.Progression, cloud.Progression) ? cloud : local;
            CloudSaveData merged = winner.Clone();
            merged.BestScore = Math.Max(local.BestScore, cloud.BestScore);
            merged.TotalKills = Math.Max(local.TotalKills, cloud.TotalKills);
			merged.RankedTotalKills = Math.Max(local.RankedTotalKills, cloud.RankedTotalKills);
			merged.RankedBestScore = Math.Max(local.RankedBestScore, cloud.RankedBestScore);
            merged.UnlockedSkins = Union(local.UnlockedSkins, cloud.UnlockedSkins);
            merged.Achievements = Union(local.Achievements, cloud.Achievements);
            merged = Normalize(merged);

            string mergedJson = merged.ToJson();
            bool changesLocal = mergedJson != Normalize(local).ToJson();
            bool changesCloud = mergedJson != Normalize(cloud).ToJson();
            return new CloudMergeResult(merged, changesLocal, changesCloud);
        }

        public static bool CloudWins(ProgressionState local, ProgressionState cloud)
        {
            if (cloud.Xp != local.Xp)
            {
                return cloud.Xp > local.Xp;
            }

            return cloud.LastLoginDay > local.LastLoginDay;
        }

        public static byte[] Serialize(CloudSaveData data) =>
            System.Text.Encoding.UTF8.GetBytes(data.ToJson());

        public static CloudSaveData Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<CloudSaveData>(System.Text.Encoding.UTF8.GetString(bytes));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private static CloudSaveData Normalize(CloudSaveData data)
        {
            CloudSaveData copy = data.Clone();
            copy.UnlockedSkins = Union(copy.UnlockedSkins, null);
            copy.Achievements = Union(copy.Achievements, null);
            return copy;
        }

        private static string[] Union(string[] first, string[] second) =>
            (first ?? Array.Empty<string>()).Concat(second ?? Array.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
    }
}
