using UnityEngine;

namespace PlanetIO
{
    public static class DirectionArrowRules
    {
		public const float ScreenHeightFraction = 0.0225f;

		public static float ClearanceRadius(float bodyRadius, float spriteRadius, float overlayRadius = 0f)
		{
			float bodyClearance = Mathf.Max(0f, bodyRadius, spriteRadius) * 1.12f;

			return Mathf.Max(bodyClearance, overlayRadius);
		}

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
