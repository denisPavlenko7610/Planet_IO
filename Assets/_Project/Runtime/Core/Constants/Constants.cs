using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public static class Constants
    {
        public const float ScaleMultiplier = 100f;
        public const float MinimumDirectionSquaredMagnitude = 0.0001f;
        public const float MinimumDisplayCapacity = 0.01f;

        public static readonly Rect WorldBounds = new(-220f, -136f, 440f, 296f);

        public static int CapacityToScore(float capacity) => Mathf.RoundToInt(capacity * ScaleMultiplier);

        public static Vector3 ClampToWorld(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, WorldBounds.xMin, WorldBounds.xMax);
            position.y = Mathf.Clamp(position.y, WorldBounds.yMin, WorldBounds.yMax);
            return position;
        }

        public static Vector2 RandomWorldPosition(float margin = 5f)
        {
            return new Vector2(
                Random.Range(WorldBounds.xMin + margin, WorldBounds.xMax - margin),
                Random.Range(WorldBounds.yMin + margin, WorldBounds.yMax - margin));
        }

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
