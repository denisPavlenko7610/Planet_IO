using UnityEngine;

namespace PlanetIO
{
    public static class CometRules
    {
        public const float DefaultDamageFraction = 0.5f;

        public static float Damage(float planetCapacity, float fraction) =>
            Mathf.Max(0f, planetCapacity) * Mathf.Clamp01(fraction);

        public const float MinimumSpeed = 2.5f;
        public const float MaximumSpeed = 3.5f;
        public const float SpawnClearance = 55f;
        public const float TrailInterval = 1.5f;
        public const float FoodLifetime = 8f;
        public const int MaximumTrailFood = 90;
        public const float TailSeconds = 1.4f;

        public static bool IsOutsideView(Vector2 position, Vector2 playerPosition) =>
            (position - playerPosition).sqrMagnitude > SpawnClearance * SpawnClearance;

        public static bool CanLeaveFood(int count, Vector2 position) =>
            count < MaximumTrailFood && WorldBounds.Area.Contains(position);

        public static Vector2 FoodPosition(Vector2 position, Vector2 direction, float radius) =>
            position - direction.normalized * (Mathf.Max(0f, radius) + 0.4f);
    }
}
