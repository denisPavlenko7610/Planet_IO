using UnityEngine;

namespace PlanetIO
{
    public enum CosmicEventType : byte
    {
        None,
        MeteorShower,
        Supernova,
        BlackHole
    }

    public readonly struct CosmicEvent
    {
        public readonly int Index;
        public readonly CosmicEventType Type;
        public readonly double StartTime;
        public readonly double DurationSeconds;
        public readonly Vector2 Position;
        public readonly Vector2 Direction;
        public readonly float Speed;

        public CosmicEvent(
            int index,
            CosmicEventType type,
            double startTime,
            double durationSeconds,
            Vector2 position,
            Vector2 direction,
            float speed)
        {
            Index = index;
            Type = type;
            StartTime = startTime;
            DurationSeconds = durationSeconds;
            Position = position;
            Direction = direction;
            Speed = speed;
        }

        public double WarningTime => StartTime - CosmicEventRules.WarningSeconds;

        public double EndTime => StartTime + DurationSeconds;

        public Vector2 PositionAt(double time)
        {
            double elapsed = System.Math.Max(0.0, System.Math.Min(time - StartTime, DurationSeconds));
            return Position + Direction * (Speed * (float)elapsed);
        }
    }

    public static class CosmicEventRules
    {
        public const double FirstEventDelay = 60.0;
        public const double Period = 120.0;
        public const double MaxJitter = 15.0;
        public const double WarningSeconds = 6.0;
        public const double MeteorSeconds = 25.0;
        public const double SupernovaSeconds = 30.0;
        public const double BlackHoleSeconds = 40.0;
        public const float MeteorSpeed = 8f;
        public const float BlackHoleSpeed = 2.5f;
        public const float BlackHoleRadius = 2.5f;
        public const float BlackHoleZoneRadius = 22f;
        public const float BlackHoleStrength = 2f;
        public const float BlackHoleMassFraction = 0.25f;
        public const float MeteorBandHalfWidth = 14f;

        private const float WorldMargin = 30f;

        public static CosmicEvent Get(int seed, int index)
        {
            int safeIndex = Mathf.Max(0, index);
            var random = new System.Random(Hash(seed, safeIndex));
            double jitter = (random.NextDouble() * 2.0 - 1.0) * MaxJitter;
            double start = FirstEventDelay + safeIndex * Period + jitter;
            CosmicEventType type = TypeFor(seed, safeIndex);
            Rect bounds = WorldBounds.Area;

            switch (type)
            {
                case CosmicEventType.MeteorShower:
                {
                    float angle = (float)(random.Next(8) * 45.0 + random.NextDouble() * 20.0 - 10.0) * Mathf.Deg2Rad;
                    Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 center = RandomPoint(random, bounds, 0.4f);
                    return new CosmicEvent(safeIndex, type, start, MeteorSeconds, center, direction, MeteorSpeed);
                }

                case CosmicEventType.Supernova:
                    return new CosmicEvent(
                        safeIndex, type, start, SupernovaSeconds, RandomPoint(random, bounds, 0.7f), Vector2.zero, 0f);

                default:
                {
                    Vector2 origin = RandomPoint(random, bounds, 0.9f);
                    Vector2 toCenter = -origin;
                    Vector2 direction = toCenter.sqrMagnitude > 1f ? toCenter.normalized : Vector2.right;
                    return new CosmicEvent(
                        safeIndex, CosmicEventType.BlackHole, start, BlackHoleSeconds, origin, direction, BlackHoleSpeed);
                }
            }
        }

        public static CosmicEventType TypeFor(int seed, int index)
        {
            int block = index / 3;
            var order = new[] { 1, 2, 3 };
            var random = new System.Random(Hash(seed, block + 7919));
            for (int slot = order.Length - 1; slot > 0; slot--)
            {
                int swap = random.Next(slot + 1);
                (order[slot], order[swap]) = (order[swap], order[slot]);
            }

            return (CosmicEventType)order[index % 3];
        }

        public static int IndexNear(double time)
        {
            return Mathf.Max(0, (int)System.Math.Round((time - FirstEventDelay) / Period));
        }

        public static bool TryFindActive(int seed, double time, out CosmicEvent found)
        {
            int center = IndexNear(time);
            for (int index = Mathf.Max(0, center - 1); index <= center + 1; index++)
            {
                CosmicEvent candidate = Get(seed, index);
                if (time >= candidate.WarningTime && time <= candidate.EndTime)
                {
                    found = candidate;
                    return true;
                }
            }

            found = default;
            return false;
        }

        private static Vector2 RandomPoint(System.Random random, Rect bounds, float spanFraction)
        {
            float halfWidth = Mathf.Max(1f, (bounds.width * 0.5f - WorldMargin) * spanFraction);
            float halfHeight = Mathf.Max(1f, (bounds.height * 0.5f - WorldMargin) * spanFraction);
            return new Vector2(
                bounds.center.x + (float)(random.NextDouble() * 2.0 - 1.0) * halfWidth,
                bounds.center.y + (float)(random.NextDouble() * 2.0 - 1.0) * halfHeight);
        }

        private static int Hash(int seed, int value)
        {
            unchecked
            {
                return (seed * 73856093) ^ (value * 19349663) ^ 0x5bd1e995;
            }
        }
    }
}
