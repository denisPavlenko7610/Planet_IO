using UnityEngine;

namespace PlanetIO
{
    public static class BoostDropRules
    {
        public const float EdgeClearance = 0.3f;

        public static Vector2 SpawnPosition(Vector2 center, Vector2 travelDirection, float bodyRadius)
        {
            Vector2 direction = travelDirection.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude
                ? travelDirection.normalized
                : Vector2.right;

            return center - direction * (Mathf.Max(0f, bodyRadius) + EdgeClearance);
        }
    }
}
