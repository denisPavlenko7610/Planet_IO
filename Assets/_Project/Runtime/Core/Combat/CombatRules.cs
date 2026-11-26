using UnityEngine;

namespace PlanetIO
{
    public static class CombatRules
    {
        public const float DefaultEatSizeRatio = 1.08f;
        public const float LeaderBountyMultiplier = 1.25f;
        public const float LootMassFraction = 0.6f;

        public static bool CanDevour(float attackerCapacity, float victimCapacity, float eatSizeRatio)
        {
            return attackerCapacity >= victimCapacity * Mathf.Max(1f, eatSizeRatio);
        }

        public static float AbsorbedMass(float victimCapacity, float absorbFraction)
        {
            return Mathf.Max(0f, victimCapacity) * Mathf.Clamp01(absorbFraction);
        }

		public static float LootSourceCapacity(float lostMass)
		{
			return Mathf.Max(0f, lostMass) / LootMassFraction;
		}

		public static bool InContact(Vector2 firstCenter, float firstRadius, Vector2 secondCenter, float secondRadius)
		{
			float reach = Mathf.Max(0f, firstRadius) + Mathf.Max(0f, secondRadius);

			return (firstCenter - secondCenter).sqrMagnitude <= reach * reach;
		}
    }
}
