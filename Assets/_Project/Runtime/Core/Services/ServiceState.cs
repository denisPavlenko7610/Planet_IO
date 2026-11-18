using System;
using System.Collections.Generic;
using System.Linq;

namespace PlanetIO
{
    [Serializable]
    public sealed class ServiceState
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int RunsFinished;
        public int FirstRunDay;
        public int ReviewPrompts;
        public int LastReviewDay;
        public int TotalKills;
		public int RankedTotalKills;
		public int RankedBestScore;
		public long RankedLastRunId = -1;
		public int RankedLastRunKills;
        public long LastRunId = -1;
        public int LastRunKills;
        public string[] Achievements = Array.Empty<string>();

        public bool IsValid() =>
            Version == CurrentVersion &&
            RunsFinished >= 0 &&
            ReviewPrompts >= 0 &&
            TotalKills >= 0 &&
			RankedTotalKills >= 0 &&
			RankedBestScore >= 0 &&
			RankedBestScore <= ScoreValidationRules.MaxScore &&
			RankedLastRunKills >= 0 &&
            LastRunKills >= 0 &&
            Achievements != null &&
            Achievements.All(id => !string.IsNullOrWhiteSpace(id));

		public bool RegisterDeath(RunSummary summary, int today, bool countKills = true)
		{
            bool newRun = summary.RunId != LastRunId;
            if (newRun)
            {
                LastRunId = summary.RunId;
                LastRunKills = 0;
                RunsFinished++;
                if (FirstRunDay <= 0)
                {
                    FirstRunDay = today;
                }
            }

			if (!countKills)
			{
				return newRun;
			}

            int kills = Math.Max(0, summary.Kills);
            TotalKills += Math.Max(0, kills - LastRunKills);
            LastRunKills = Math.Max(LastRunKills, kills);
            return newRun;
        }


		public void RegisterRankedRun(RunSummary summary)
		{
			if (RankedLastRunId != summary.RunId)
			{
				RankedLastRunId = summary.RunId;
				RankedLastRunKills = 0;
			}

			RankedTotalKills += Math.Max(0, summary.Kills - RankedLastRunKills);
			RankedLastRunKills = Math.Max(RankedLastRunKills, summary.Kills);
			RankedBestScore = Math.Max(RankedBestScore, summary.PeakScore);
		}

        public void MarkReviewPrompted(int today)
        {
            ReviewPrompts++;
            LastReviewDay = today;
        }

        public bool HasAchievement(AchievementKind kind) => Array.IndexOf(Achievements, kind.ToString()) >= 0;

        public bool AddAchievement(AchievementKind kind)
        {
            if (HasAchievement(kind))
            {
                return false;
            }

            var list = new List<string>(Achievements) { kind.ToString() };
            list.Sort(StringComparer.Ordinal);
            Achievements = list.ToArray();
            return true;
        }

        public void MergeAchievements(IEnumerable<string> ids)
        {
            if (ids == null)
            {
                return;
            }

            Achievements = Achievements.Concat(ids).Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
        }
    }
}
