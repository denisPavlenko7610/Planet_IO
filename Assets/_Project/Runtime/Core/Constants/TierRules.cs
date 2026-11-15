namespace PlanetIO
{
    public static class TierRules
    {
        public const int MinTier = 1;
        public const int MaxTier = 4;
        public const float DowngradeMargin = 0.03f;

        private const float TierTwoCapacity = 0.35f;
        private const float TierThreeCapacity = 0.8f;
        private const float TierFourCapacity = 1.4f;

        public static float LowerBound(int tier) => tier switch
        {
            <= MinTier => 0f,
            2 => TierTwoCapacity,
            3 => TierThreeCapacity,
            _ => TierFourCapacity
        };

        public static int Resolve(float capacity, int currentTier)
        {
            int tier = currentTier < MinTier ? MinTier : currentTier > MaxTier ? MaxTier : currentTier;

            while (tier < MaxTier && capacity >= LowerBound(tier + 1))
            {
                tier++;
            }

            while (tier > MinTier && capacity < LowerBound(tier) - DowngradeMargin)
            {
                tier--;
            }

            return tier;
        }

        public static float Progress(float capacity, int tier)
        {
            if (tier >= MaxTier)
            {
                return 1f;
            }

            if (float.IsNaN(capacity))
            {
                return 0f;
            }

            float lower = LowerBound(tier);
            float upper = LowerBound(tier + 1);
            return UnityEngine.Mathf.Clamp01((capacity - lower) / (upper - lower));
        }

        public static int MoonSlots(int tier) => tier switch
        {
            <= MinTier => 0,
            2 => 2,
            3 => 4,
            _ => 6
        };
    }
}
