using UnityEngine;

namespace PlanetIO
{
    public static class DustRules
    {
        public const int SkinPrice = 250;
        public const int DoubleMultiplier = 2;
        public const int StreakBaseReward = 10;
        public const int StreakStepReward = 5;
        public const int StreakMaxReward = 50;

        public static int RunDust(int xp) => xp <= 0 ? 0 : Mathf.Max(1, xp / 4);

        public static int StreakReward(int streakDays)
        {
            int days = Mathf.Max(1, streakDays);
            return Mathf.Min(StreakMaxReward, StreakBaseReward + StreakStepReward * (days - 1));
        }
    }
}
