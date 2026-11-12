using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class FoodFieldMapTests
    {
        private const int TotalFood = 1800;
        private const int Seed = 12345;

        private static readonly Rect Bounds = Rect.MinMaxRect(-210f, -125f, 210f, 150f);

        private static FoodFieldMap CreateMap(int seed = Seed)
        {
            return new FoodFieldMap(Bounds, TotalFood, new FoodFieldSettings(), seed);
        }

        private static bool IsInsideBounds(Vector2 position)
        {
            return position.x >= Bounds.xMin && position.x <= Bounds.xMax &&
                position.y >= Bounds.yMin && position.y <= Bounds.yMax;
        }

        private static void FillCompletely(FoodFieldMap map)
        {
            for (int index = 0; index < TotalFood; index++)
            {
                map.Reserve(FoodFieldMap.NoCell);
            }
        }

        [Test]
        public void Targets_SumToTotalFood()
        {
            FoodFieldMap map = CreateMap();

            float sum = 0f;
            for (int cell = 0; cell < map.CellCount; cell++)
            {
                sum += map.GetTarget(cell);
            }

            Assert.That(sum, Is.EqualTo(TotalFood).Within(0.5f));
        }

        [Test]
        public void HomeField_IsRicherThanAverageCell()
        {
            FoodFieldMap map = CreateMap();

            float average = (float)TotalFood / map.CellCount;
            float home = map.GetTarget(map.CellOf(Vector2.zero));

            Assert.That(home, Is.GreaterThan(average * 3f));
        }

        [Test]
        public void Quotas_SumToTotalFoodAndStayWithinOneOfTargets()
        {
            FoodFieldMap map = CreateMap();

            int sum = 0;
            for (int cell = 0; cell < map.CellCount; cell++)
            {
                sum += map.GetQuota(cell);
                Assert.That(map.GetQuota(cell), Is.InRange(Mathf.FloorToInt(map.GetTarget(cell)) - 1, Mathf.CeilToInt(map.GetTarget(cell)) + 1));
            }

            Assert.That(sum, Is.EqualTo(TotalFood));
        }

        [Test]
        public void FullFill_MatchesQuotasExactly()
        {
            FoodFieldMap map = CreateMap();

            FillCompletely(map);

            Assert.That(map.PlacedCount, Is.EqualTo(TotalFood));
            for (int cell = 0; cell < map.CellCount; cell++)
            {
                Assert.That(map.GetCount(cell), Is.EqualTo(map.GetQuota(cell)));
            }
        }

        [Test]
        public void DepletedCell_AttractsTheNextRespawns()
        {
            FoodFieldMap map = CreateMap();
            FillCompletely(map);
            int home = map.CellOf(Vector2.zero);
            int farCell = map.CellOf(new Vector2(-190f, -110f));
            int removed = Mathf.Min(map.GetCount(home), 6);
            Assert.That(removed, Is.GreaterThan(0));

            for (int index = 0; index < removed; index++)
            {
                map.Release(home);
            }

            for (int index = 0; index < removed; index++)
            {
                Assert.That(map.Reserve(farCell), Is.EqualTo(home));
            }
        }

        [Test]
        public void Release_FreesCountAndIgnoresInvalidCells()
        {
            FoodFieldMap map = CreateMap();
            int cell = map.Reserve(FoodFieldMap.NoCell);

            map.Release(cell);
            map.Release(FoodFieldMap.NoCell);
            map.Release(map.CellCount + 5);

            Assert.That(map.GetCount(cell), Is.EqualTo(0));
            Assert.That(map.PlacedCount, Is.EqualTo(0));
        }

        [Test]
        public void Respawn_MostlyMovesAwayFromEatenNeighbourhood()
        {
            FoodFieldMap map = CreateMap();
            FillCompletely(map);
            int source = map.CellOf(Vector2.zero);
            map.Release(source);

            int near = 0;
            const int trials = 300;
            for (int trial = 0; trial < trials; trial++)
            {
                int cell = map.Reserve(source);
                if (Mathf.Abs(cell % 35 - source % 35) <= 1 && Mathf.Abs(cell / 35 - source / 35) <= 1)
                {
                    near++;
                }

                map.Release(cell);
            }

            Assert.That((float)near / trials, Is.InRange(0.15f, 0.45f));
        }

        [Test]
        public void SamplePosition_StaysInsideBounds()
        {
            FoodFieldMap map = CreateMap();

            for (int cell = 0; cell < map.CellCount; cell++)
            {
                for (int sample = 0; sample < 12; sample++)
                {
                    Vector2 position = map.SamplePosition(cell);
                    Assert.That(IsInsideBounds(position), Is.True, $"cell {cell} produced {position}");
                }
            }
        }

        [Test]
        public void RelocateField_KeepsHomeFieldAndTotalTarget()
        {
            FoodFieldMap map = CreateMap();
            Vector2 home = map.GetFieldCenter(0);

            for (int index = 0; index < 60; index++)
            {
                Assert.That(map.RelocateField(), Is.True);
            }

            float sum = 0f;
            for (int cell = 0; cell < map.CellCount; cell++)
            {
                sum += map.GetTarget(cell);
            }

            Assert.That(map.GetFieldCenter(0), Is.EqualTo(home));
            Assert.That(map.FieldCount, Is.EqualTo(new FoodFieldSettings().FieldCount + 1));
            Assert.That(sum, Is.EqualTo(TotalFood).Within(0.5f));
        }

        [Test]
        public void SameSeed_ProducesSameLayout()
        {
            FoodFieldMap first = CreateMap(99);
            FoodFieldMap second = CreateMap(99);

            for (int index = 0; index < first.FieldCount; index++)
            {
                Assert.That(first.GetFieldCenter(index), Is.EqualTo(second.GetFieldCenter(index)));
            }
        }

        [Test]
        public void TryGetFieldTarget_RespectsMinimumDistanceAndBounds()
        {
            FoodFieldMap map = CreateMap();

            bool found = map.TryGetFieldTarget(new Vector2(150f, 100f), 10f, out Vector2 target);

            Assert.That(found, Is.True);
            Assert.That(IsInsideBounds(target), Is.True);
        }

        [Test]
        public void TryGetFieldTarget_FailsWhenEveryFieldIsTooClose()
        {
            FoodFieldMap map = CreateMap();

            Assert.That(map.TryGetFieldTarget(Vector2.zero, 100000f, out _), Is.False);
        }

        [TestCase(0.01f, 0f, 0.45f)]
        [TestCase(0.01f, 1f, 0.65f)]
        [TestCase(0.15f, 0f, 0.22f)]
        [TestCase(0.15f, 1f, 0.32f)]
        [TestCase(0.9f, 0f, 0.1f)]
        [TestCase(0.9f, 1f, 0.16f)]
        public void SizeTiers_MapRollsToTierRanges(float tierRoll, float scaleRoll, float expected)
        {
            FoodSizeTiers tiers = new();

            Assert.That(tiers.Resolve(tierRoll, scaleRoll), Is.EqualTo(expected).Within(1e-5f));
        }

        [Test]
        public void SizeTiers_GoldenAndDroppedStayInTheirRanges()
        {
            FoodSizeTiers tiers = new();

            Assert.That(tiers.ResolveGolden(0f), Is.EqualTo(0.28f).Within(1e-5f));
            Assert.That(tiers.ResolveGolden(1f), Is.EqualTo(0.38f).Within(1e-5f));
            Assert.That(tiers.ResolveDropped(0f), Is.EqualTo(0.16f).Within(1e-5f));
            Assert.That(tiers.ResolveDropped(1f), Is.EqualTo(0.24f).Within(1e-5f));
        }
    }
}
