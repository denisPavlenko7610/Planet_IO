using UnityEngine;

namespace PlanetIO
{
    public static class CombatRules
    {
        public const float DefaultEatSizeRatio = 1.08f;
        public const float LeaderBountyMultiplier = 1.25f;

        public static bool CanDevour(float attackerCapacity, float victimCapacity, float eatSizeRatio)
        {
            return attackerCapacity >= victimCapacity * Mathf.Max(1f, eatSizeRatio);
        }

        public static float AbsorbedMass(float victimCapacity, float absorbFraction)
        {
            return Mathf.Max(0f, victimCapacity) * Mathf.Clamp01(absorbFraction);
        }
    }
}
