using UnityEngine;

namespace PlanetIO
{
    public static class FoodMagnetRules
    {
        public const float ReferenceBodyRadius = 0.28f;
        public const float ReachGrowthExponent = 0.7f;
        public const float OwnDropGraceSeconds = 1f;
		public const float BaseReach = 0.2f;
		public const float PullSpeed = 5f;

        public static float Reach(float bodyRadius, float baseReach, float perkMultiplier)
        {
            float growth = Mathf.Pow(
                Mathf.Max(1f, bodyRadius / ReferenceBodyRadius),
                ReachGrowthExponent);
            return bodyRadius + baseReach * Mathf.Max(0f, perkMultiplier) * growth;
        }
    }
}
