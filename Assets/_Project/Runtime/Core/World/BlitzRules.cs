using UnityEngine;

namespace PlanetIO
{
    public static class BlitzRules
    {
        public const float DurationSeconds = 300f;

        public static float Remaining(float elapsedSeconds) =>
            Mathf.Max(0f, DurationSeconds - Mathf.Max(0f, elapsedSeconds));

        public static bool IsOver(float elapsedSeconds) => elapsedSeconds >= DurationSeconds;

        public static string FormatClock(float remainingSeconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
            return $"{total / 60}:{total % 60:00}";
        }

        public static int RankOf(float ownCapacity, float[] capacities)
        {
            int rank = 1;
            foreach (float other in capacities)
            {
                if (other > ownCapacity)
                {
                    rank++;
                }
            }

            return rank;
        }
    }
}
