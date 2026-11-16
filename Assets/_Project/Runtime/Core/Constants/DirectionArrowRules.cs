using UnityEngine;

namespace PlanetIO
{
    public static class DirectionArrowRules
    {
        public static float WorldSize(float orthographicSize, float screenHeightFraction)
        {
            return Mathf.Max(0f, orthographicSize) * 2f * Mathf.Max(0f, screenHeightFraction);
        }

        public static float Distance(float bodyRadius, float arrowWorldSize, float edgeGapInArrowSizes)
        {
            return Mathf.Max(0f, bodyRadius) + arrowWorldSize * (0.5f + Mathf.Max(0f, edgeGapInArrowSizes));
        }
    }
}
