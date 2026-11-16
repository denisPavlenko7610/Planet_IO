using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    [AutoStaticsCleanup]
    public static partial class FoodViewScale
    {
        public const float BaseBoost = 1.2f;
        public const float ZoomExponent = 0.7f;
        public const float MaximumFactor = 3f;
        private const float Step = 0.05f;
        private const float ChangeThreshold = 0.001f;

        public static float Factor { get; private set; } = BaseBoost;

        public static event Action<float> Changed;

        public static float Resolve(float zoomRatio)
        {
            float factor = BaseBoost * Mathf.Pow(Mathf.Max(1f, zoomRatio), ZoomExponent);
            return Mathf.Min(MaximumFactor, factor);
        }

        public static void SetZoomRatio(float zoomRatio)
        {
            float quantized = Mathf.Round(Resolve(zoomRatio) / Step) * Step;
            if (Mathf.Abs(quantized - Factor) < ChangeThreshold)
            {
                return;
            }

            Factor = quantized;
            Changed?.Invoke(Factor);
        }
    }
}
