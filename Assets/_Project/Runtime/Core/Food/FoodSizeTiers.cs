using System;
using UnityEngine;

namespace PlanetIO
{
    [Serializable]
    public sealed class FoodSizeTiers
    {
        [SerializeField] private Vector2 _small = new(0.1f, 0.16f);
        [SerializeField] private Vector2 _medium = new(0.22f, 0.32f);
        [SerializeField] private Vector2 _large = new(0.45f, 0.65f);
        [SerializeField] private Vector2 _golden = new(0.28f, 0.38f);
        [SerializeField] private Vector2 _dropped = new(0.16f, 0.24f);
        [SerializeField, Range(0f, 1f)] private float _mediumChance = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _largeChance = 0.05f;

        public float Resolve(float tierRoll, float scaleRoll)
        {
            float largeChance = Mathf.Clamp01(_largeChance);
            float mediumChance = Mathf.Clamp01(_mediumChance);

            if (tierRoll < largeChance)
            {
                return Pick(_large, scaleRoll);
            }

            return tierRoll < largeChance + mediumChance
                ? Pick(_medium, scaleRoll)
                : Pick(_small, scaleRoll);
        }

        public float ResolveGolden(float scaleRoll)
        {
            return Pick(_golden, scaleRoll);
        }

        public float ResolveDropped(float scaleRoll)
        {
            return Pick(_dropped, scaleRoll);
        }

        private static float Pick(Vector2 range, float scaleRoll)
        {
            float minimum = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Mathf.Lerp(minimum, maximum, Mathf.Clamp01(scaleRoll));
        }
    }
}
