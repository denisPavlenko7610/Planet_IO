using UnityEngine;

namespace PlanetIO
{
    public static class XpRules
    {
        public const int MaxRank = 30;
        public const float MaxCountedSeconds = 900f;
        public const int KillXp = 25;
        public const int TierXp = 20;

        private const int BaseStep = 100;
        private const int StepGrowth = 40;

        public static int RunXp(RunSummary summary)
        {
            int time = Mathf.FloorToInt(Mathf.Clamp(summary.DurationSeconds, 0f, MaxCountedSeconds) * 0.5f);
            int mass = Mathf.Max(0, summary.PeakScore) / 2;
            int kills = Mathf.Max(0, summary.Kills) * KillXp;
            int tier = Mathf.Max(0, summary.PeakTier - 1) * TierXp;
            return time + mass + kills + tier;
        }

        public static int StepXp(int rank) => BaseStep + StepGrowth * (Mathf.Clamp(rank, 1, MaxRank) - 1);

        public static int RankStartXp(int rank)
        {
            int clamped = Mathf.Clamp(rank, 1, MaxRank);
            int completed = clamped - 1;
            return completed * BaseStep + StepGrowth * completed * (completed - 1) / 2;
        }

        public static int RankForXp(int xp)
        {
            int rank = 1;
            while (rank < MaxRank && xp >= RankStartXp(rank + 1))
            {
                rank++;
            }

            return rank;
        }

        public static float Progress(int xp)
        {
            int rank = RankForXp(xp);
            if (rank >= MaxRank)
            {
                return 1f;
            }

            return Mathf.Clamp01((xp - RankStartXp(rank)) / (float)StepXp(rank));
        }
    }
}
