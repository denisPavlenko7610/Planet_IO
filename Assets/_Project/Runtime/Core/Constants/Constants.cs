using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public static class Constants
    {
        public const float ScaleMultiplier = 100f;
        public const float MinimumDirectionSquaredMagnitude = 0.0001f;
        public const ulong UnassignedClientId = ulong.MaxValue;
        public const string FoodLayerName = "Food";

        public static int CapacityToScore(float capacity) => Mathf.RoundToInt(capacity * ScaleMultiplier);

        public static Quaternion DirectionToRotation(Vector2 direction)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.Euler(0f, 0f, angle);
        }

        public static Vector2 GetRandomDirection() => NormalizedOrRight(Random.insideUnitCircle);

        public static Vector2 NormalizedOrRight(Vector2 vector)
        {
            return vector.sqrMagnitude > MinimumDirectionSquaredMagnitude
                ? vector.normalized
                : Vector2.right;
        }

        public static Vector2 NormalizedOrRandom(Vector2 vector)
        {
            return vector.sqrMagnitude > MinimumDirectionSquaredMagnitude
                ? vector.normalized
                : GetRandomDirection();
        }
    }
}
