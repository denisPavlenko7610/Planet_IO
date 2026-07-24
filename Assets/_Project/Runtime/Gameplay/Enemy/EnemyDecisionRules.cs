using UnityEngine;

namespace PlanetIO
{
    public enum EnemyIntent : byte
    {
        Roam,
        Forage,
        Hunt,
        Evade
    }

    public static class EnemyDecisionRules
    {
        public static EnemyIntent ChooseIntent(float ownCapacity, float nearestPlayerCapacity, bool hasVisibleFood,
            bool hasImmediateHazard, float huntSizeRatio, float threatSizeRatio)
		{
            if (hasImmediateHazard)
            {
                return EnemyIntent.Evade;
            }

            if (nearestPlayerCapacity > 0f)
            {
                if (nearestPlayerCapacity >=
                    ownCapacity * Mathf.Max(1f, threatSizeRatio))
                {
                    return EnemyIntent.Evade;
                }

                if (ownCapacity >=
                    nearestPlayerCapacity * Mathf.Max(1f, huntSizeRatio))
                {
                    return EnemyIntent.Hunt;
                }
            }

            return hasVisibleFood
                ? EnemyIntent.Forage
                : EnemyIntent.Roam;
        }

        public static float GetCapacitySpeedMultiplier(
            float capacity,
            float minimumCapacity,
            float massPenalty,
            float minimumMultiplier)
        {
            float sizeAboveMinimum = Mathf.Max(0f, capacity - minimumCapacity);
            return Mathf.Clamp(
                1f - sizeAboveMinimum * Mathf.Max(0f, massPenalty),
                Mathf.Clamp01(minimumMultiplier),
                1f);
        }
    }
}
