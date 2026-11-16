using UnityEngine;

namespace PlanetIO
{
    public static class PlanetSizeRules
    {
        public const float ReferenceCapacity = 0.08f;
        public const float AreaExponent = 0.5f;
        private const float MinimumCapacity = 0.0001f;

        public static float Scale(float capacity)
        {
            float ratio = Mathf.Max(capacity, MinimumCapacity) / ReferenceCapacity;
            return ReferenceCapacity * Mathf.Pow(ratio, AreaExponent);
        }
    }
}
