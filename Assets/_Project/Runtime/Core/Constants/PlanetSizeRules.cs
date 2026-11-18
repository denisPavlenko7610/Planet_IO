using UnityEngine;

namespace PlanetIO
{
    public static class PlanetSizeRules
    {
        public const float ReferenceCapacity = 0.08f;
        public const float MaximumCapacity = 8f;
        public const float AreaExponent = 0.5f;
        private const float MinimumCapacity = 0.0001f;

		public const float GrowthSettleSeconds = 0.45f;

		public static float SmoothGrowth(float current, float target, float deltaTime)
		{
			if (target <= current) return target;
			float blend = 1f - Mathf.Exp(-3f * Mathf.Max(0f, deltaTime) / GrowthSettleSeconds);
			return Mathf.Lerp(current, target, blend);
		}

        public static float Scale(float capacity)
        {
            float ratio = Mathf.Max(capacity, MinimumCapacity) / ReferenceCapacity;
            return ReferenceCapacity * Mathf.Pow(ratio, AreaExponent);
        }
    }
}
