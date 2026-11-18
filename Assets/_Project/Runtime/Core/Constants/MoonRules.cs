using UnityEngine;

namespace PlanetIO
{
    public static class MoonRules
    {
        public const float OrbitRadiusFactor = 1.7f;
        public const float MoonRadiusFactor = 0.2f;
        public const float AngularSpeedDegrees = 90f;
        public const int MaxDurability = 2;
        public const float RegrowSeconds = 16f;
        public const float HitDamageFraction = 0.10f;
        public const float MinHitDamage = 0.004f;
        public const float HitImmunitySeconds = 1f;
        public const float OwnerGainFraction = 0.5f;
        public const float KnockbackSpeed = 6f;
        public const float ClashKnockbackSpeed = 3f;
        public const float ClashCooldownSeconds = 0.8f;
        public const float ClashSearchPadding = 16f;
        public const float BotThreatMargin = 0.3f;

        public static float OrbitRadius(float planetRadius) => planetRadius * OrbitRadiusFactor;

        public static float OuterExtent(float planetRadius) =>
            planetRadius * (OrbitRadiusFactor + MoonRadiusFactor);

        public static float MoonRadius(float planetRadius) => planetRadius * MoonRadiusFactor;

        public static float Damage(float victimCapacity) =>
            Mathf.Max(MinHitDamage, victimCapacity * HitDamageFraction);

        public static Vector2 Position(
            Vector2 center,
            float planetRadius,
            int index,
            int slots,
            double time,
            float phaseDegrees)
        {
            int count = Mathf.Max(1, slots);
            double degrees = 360.0 * index / count + phaseDegrees + AngularSpeedDegrees * time;
            float radians = (float)(degrees % 360.0) * Mathf.Deg2Rad;
            float orbit = OrbitRadius(planetRadius);
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * orbit;
        }
    }
}
