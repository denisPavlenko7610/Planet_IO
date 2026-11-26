using UnityEngine;

namespace PlanetIO
{
    public readonly struct MapDisc
    {
        public readonly Vector2 Center;
        public readonly float Radius;

        public MapDisc(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public bool Contains(Vector2 point) => (point - Center).sqrMagnitude <= Radius * Radius;
    }

    public static class MapFeatureRules
    {
        public const int StarCount = 4;
        public const int NebulaCount = 3;
        public const float StarRadius = 3f;
        public const float StarZoneRadius = 14f;
        public const float StarStrength = 0.8f;
        public const float WormholeRadius = 2.5f;
        public const float WormholeCooldownSeconds = 10f;
        public const float WormholeExitOffset = 5f;
        public const float HomeClearRadius = 40f;
        public const float StarSeparation = 90f;
        public const float WormholeSeparation = 200f;

        private const float EdgeMargin = 35f;
        private const int Attempts = 300;

        public static void Stars(int seed, Vector2[] result)
        {
            var random = new System.Random(seed * 40503 + 11);
            int placed = 0;
            for (int attempt = 0; attempt < Attempts && placed < result.Length; attempt++)
            {
                Vector2 point = RandomPoint(random);
                if (point.magnitude < HomeClearRadius || !FarFromAll(point, result, placed, StarSeparation))
                {
                    continue;
                }

                result[placed++] = point;
            }

			Rect bounds = WorldBounds.Area;
            Vector2[] fallback =
            {
                new(bounds.xMin + 80f, bounds.yMax - 60f),
                new(bounds.xMax - 80f, bounds.yMax - 60f),
                new(bounds.xMin + 80f, bounds.yMin + 60f),
                new(bounds.xMax - 80f, bounds.yMin + 60f)
            };
            for (int index = 0; placed < result.Length; index++)
            {
                result[placed++] = fallback[index % fallback.Length];
            }
        }

        public static void Wormholes(int seed, out Vector2 first, out Vector2 second)
        {
            var random = new System.Random(seed * 92821 + 3);
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                Vector2 a = RandomPoint(random);
                Vector2 b = RandomPoint(random);
                if (a.magnitude >= HomeClearRadius &&
                    b.magnitude >= HomeClearRadius &&
                    (a - b).magnitude >= WormholeSeparation)
                {
                    first = a;
                    second = b;
                    return;
                }
            }

			Rect bounds = WorldBounds.Area;
            first = new Vector2(bounds.xMin + 70f, bounds.center.y);
            second = new Vector2(bounds.xMax - 70f, bounds.center.y);
        }

        public static void Nebulae(int seed, MapDisc[] result)
        {
            var random = new System.Random(seed * 68239 + 17);
			Rect bounds = WorldBounds.Area;
            for (int index = 0; index < result.Length; index++)
            {
                float radius = 28f + (float)random.NextDouble() * 12f;
                float x = Mathf.Lerp(bounds.xMin + radius, bounds.xMax - radius, (float)random.NextDouble());
                float y = Mathf.Lerp(bounds.yMin + radius, bounds.yMax - radius, (float)random.NextDouble());
                result[index] = new MapDisc(new Vector2(x, y), radius);
            }
        }

        public static bool InsideAny(MapDisc[] discs, Vector2 point)
        {
            if (discs == null)
            {
                return false;
            }

            for (int index = 0; index < discs.Length; index++)
            {
                if (discs[index].Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 RandomPoint(System.Random random)
        {
			Rect bounds = WorldBounds.Area;
            return new Vector2(
                Mathf.Lerp(bounds.xMin + EdgeMargin, bounds.xMax - EdgeMargin, (float)random.NextDouble()),
                Mathf.Lerp(bounds.yMin + EdgeMargin, bounds.yMax - EdgeMargin, (float)random.NextDouble()));
        }

        private static bool FarFromAll(Vector2 point, Vector2[] others, int count, float distance)
        {
            for (int index = 0; index < count; index++)
            {
                if ((point - others[index]).magnitude < distance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
