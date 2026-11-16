using System;

namespace PlanetIO
{
    public enum PerkId : byte
    {
        IonTrail,
        Magnetosphere,
        HeavyMoons,
        AblativeCrust,
        SolarSail,
        GravityWell,
        NebulaVeil,
        CometRider,
        Predator
    }

    public static class PerkRules
    {
        public const int Count = 9;
        public const int OfferSize = 3;
        public const float OfferSeconds = 5f;
        public const float AblativeSurvivalFraction = 0.3f;
        public const float StealthSeconds = 10f;
        public const float StealthCooldownSeconds = 40f;
        public const float IonTrailPointSeconds = 2f;
        public const float IonTrailDamageScale = 0.7f;

        public static ushort Bit(PerkId id) => (ushort)(1 << (int)id);

        public static bool Has(ushort mask, PerkId id) => (mask & Bit(id)) != 0;

        public static ushort With(ushort mask, PerkId id) => (ushort)(mask | Bit(id));

        public static bool IsValid(byte value) => value < Count;

        public static int OwnedCount(ushort mask)
        {
            int count = 0;
            for (int index = 0; index < Count; index++)
            {
                if ((mask & (1 << index)) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        public static float SpeedMultiplier(ushort mask)
        {
            float value = 1f;
            value *= Has(mask, PerkId.Magnetosphere) ? 0.92f : 1f;
            value *= Has(mask, PerkId.HeavyMoons) ? 0.95f : 1f;
            value *= Has(mask, PerkId.SolarSail) ? 1.12f : 1f;
            value *= Has(mask, PerkId.GravityWell) ? 0.94f : 1f;
            return value;
        }

        public static float FoodMultiplier(ushort mask)
        {
            float value = 1f;
            value *= Has(mask, PerkId.SolarSail) ? 0.9f : 1f;
            value *= Has(mask, PerkId.Predator) ? 0.9f : 1f;
            return value;
        }

        public static float MagnetRadiusMultiplier(ushort mask) => Has(mask, PerkId.Magnetosphere) ? 1.5f : 1f;

        public static int MoonSlotBonus(ushort mask) => Has(mask, PerkId.HeavyMoons) ? 1 : 0;

        public static float MoonDamageMultiplier(ushort mask) => Has(mask, PerkId.HeavyMoons) ? 1.5f : 1f;

        public static float GravityZoneMultiplier(ushort mask) => Has(mask, PerkId.GravityWell) ? 1.4f : 1f;

        public static float AbsorbMultiplier(ushort mask) => Has(mask, PerkId.Predator) ? 1.2f : 1f;

        public static float CometHealFraction(ushort mask) => Has(mask, PerkId.CometRider) ? 0.5f : 0f;

        public static float BoostCostMultiplier(ushort mask) => Has(mask, PerkId.IonTrail) ? 1.25f : 1f;

        public static float BoostSpeedMultiplier(ushort mask) => Has(mask, PerkId.IonTrail) ? 1.15f : 1f;

        public static int FillOffer(ushort ownedMask, int seed, byte[] result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            Span<byte> candidates = stackalloc byte[Count];
            int candidateCount = 0;
            for (byte id = 0; id < Count; id++)
            {
                if ((ownedMask & (1 << id)) == 0)
                {
                    candidates[candidateCount++] = id;
                }
            }

            var random = new System.Random(seed);
            int picked = 0;
            while (picked < OfferSize && picked < result.Length && candidateCount > 0)
            {
                int index = random.Next(candidateCount);
                result[picked++] = candidates[index];
                candidates[index] = candidates[--candidateCount];
            }

            return picked;
        }
    }
}
