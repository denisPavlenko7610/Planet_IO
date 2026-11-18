using UnityEngine;

namespace PlanetIO
{
    public static class PlatformServiceIds
    {
        public const string CloudSnapshotName = "planetio-progress";

        public static string LeaderboardId(LeaderboardKind kind) => kind switch
        {
            LeaderboardKind.PeakMass => "CgkInbS7o_YBEAIQAQ",
            LeaderboardKind.KillsPerRun => "CgkInbS7o_YBEAIQAg",
            LeaderboardKind.TotalKills => "CgkInbS7o_YBEAIQAw",
            _ => ""
        };

        public static string AchievementId(AchievementKind kind) => kind switch
        {
            AchievementKind.TierPlanet => "CgkInbS7o_YBEAIQBA",
            AchievementKind.TierGasGiant => "CgkInbS7o_YBEAIQBQ",
            AchievementKind.TierStar => "CgkInbS7o_YBEAIQBg",
            AchievementKind.Kills5 => "CgkInbS7o_YBEAIQBw",
            AchievementKind.Kills15 => "CgkInbS7o_YBEAIQCA",
            AchievementKind.Survive5Min => "CgkInbS7o_YBEAIQCQ",
            AchievementKind.Rank5 => "CgkInbS7o_YBEAIQCg",
            AchievementKind.Rank15 => "CgkInbS7o_YBEAIQCw",
            AchievementKind.Rank30 => "CgkInbS7o_YBEAIQDA",
            AchievementKind.Streak7 => "CgkInbS7o_YBEAIQDQ",
            _ => ""
        };

        public static bool IsPlayableDevice => !UnityEngine.Application.isEditor &&
                                               UnityEngine.Application.platform == RuntimePlatform.Android;
    }
}
