using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO.Utils
{
    public static class Constants
    {
        public const float ScaleMultiplier = 100f;
        public const float MinimumDirectionSquaredMagnitude = 0.0001f;
        public const float MinimumDisplayCapacity = 0.01f;

        public static int CapacityToScore(float capacity) => Mathf.RoundToInt(capacity * ScaleMultiplier);

        public static Quaternion DirectionToRotation(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }

        public static Vector2 GetRandomDirection()
        {
            Vector2 direction = Random.insideUnitCircle;
            return direction.sqrMagnitude > MinimumDirectionSquaredMagnitude
                ? direction.normalized
                : Vector2.right;
        }
    }
}
