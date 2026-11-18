using System;

namespace PlanetIO
{
    public enum AchievementKind
    {
        TierPlanet,
        TierGasGiant,
        TierStar,
        Kills5,
        Kills15,
        Survive5Min,
        Rank5,
        Rank15,
        Rank30,
        Streak7
    }

    public enum LeaderboardKind
    {
        PeakMass,
        KillsPerRun,
        TotalKills
    }

    public static class AchievementRules
    {
        public const float SurviveSeconds = 300f;
        public const int StreakDays = 7;

        public static int Count => Enum.GetValues(typeof(AchievementKind)).Length;

        public static bool IsEarned(AchievementKind kind, RunSummary run, int rank, int streakDays) => kind switch
        {
            AchievementKind.TierPlanet => run.PeakTier >= 2,
            AchievementKind.TierGasGiant => run.PeakTier >= 3,
            AchievementKind.TierStar => run.PeakTier >= 4,
            AchievementKind.Kills5 => run.Kills >= 5,
            AchievementKind.Kills15 => run.Kills >= 15,
            AchievementKind.Survive5Min => run.DurationSeconds >= SurviveSeconds,
            AchievementKind.Rank5 => rank >= 5,
            AchievementKind.Rank15 => rank >= 15,
            AchievementKind.Rank30 => rank >= XpRules.MaxRank,
            AchievementKind.Streak7 => streakDays >= StreakDays,
            _ => false
        };
    }
}
