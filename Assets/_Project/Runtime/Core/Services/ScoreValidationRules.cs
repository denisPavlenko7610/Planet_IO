using UnityEngine;

namespace PlanetIO
{
    public static class ScoreValidationRules
    {
        public const float MinRunSeconds = 10f;
        public const float SecondsPerKill = 2f;
        public const int KillSlack = 3;

        public static int MaxScore => Constants.CapacityToScore(PlanetSizeRules.MaximumCapacity);

        public static int MaxKills(float durationSeconds) =>
            KillSlack + Mathf.FloorToInt(Mathf.Max(0f, durationSeconds) / SecondsPerKill);

        public static bool IsPlausible(RunSummary run) =>
            run.DurationSeconds >= MinRunSeconds &&
            run.PeakScore >= 0 &&
            run.PeakScore <= MaxScore &&
            run.Kills >= 0 &&
            run.Kills <= MaxKills(run.DurationSeconds);

        public static bool CountsForRanking(BotDifficulty difficulty) => difficulty != BotDifficulty.Easy;
    }
}
