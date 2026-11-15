using UnityEngine;

namespace PlanetIO
{
    public static class GravityRules
    {
        public const float ZoneFactor = 3f;
        public const float AffectedMassRatio = 0.6f;
        public const float MaxPullSpeed = 1.4f;
        public const float TangentMinDegrees = 60f;
        public const float TangentMaxDegrees = 120f;
        public const float MinSlingshotSpeed = 1f;

        public static float ZoneRadius(float bodyRadius, float zoneMultiplier) =>
            bodyRadius * ZoneFactor * Mathf.Max(1f, zoneMultiplier);

        public static bool Affects(float wellCapacity, float bodyCapacity) =>
            bodyCapacity <= wellCapacity * AffectedMassRatio;

        public static float PullSpeed(float distance, float wellRadius, float zoneRadius)
        {
            if (zoneRadius <= wellRadius || distance >= zoneRadius)
            {
                return 0f;
            }

            float falloff = Mathf.Clamp01(1f - (distance - wellRadius) / (zoneRadius - wellRadius));
            return MaxPullSpeed * falloff * falloff;
        }

        public static bool IsTangential(Vector2 velocity, Vector2 towardWell)
        {
            if (velocity.sqrMagnitude < MinSlingshotSpeed * MinSlingshotSpeed ||
                towardWell.sqrMagnitude < Constants.MinimumDirectionSquaredMagnitude)
            {
                return false;
            }

            float angle = Vector2.Angle(velocity, towardWell);
            return angle >= TangentMinDegrees && angle <= TangentMaxDegrees;
        }
    }
}
