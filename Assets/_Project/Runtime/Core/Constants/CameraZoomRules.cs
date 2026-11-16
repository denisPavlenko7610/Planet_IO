using UnityEngine;

namespace PlanetIO
{
    public static class CameraZoomRules
    {
        public static float Ratio(float capacity, float exponent)
        {
            float growth = Mathf.Max(1f, capacity / PlanetSizeRules.ReferenceCapacity);
            return Mathf.Pow(growth, Mathf.Max(0f, exponent));
        }
    }
}
