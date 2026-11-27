using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class WorldRulesTests
    {
        private static readonly int[] Seeds = { 0, 1, 42, 20000, -77, int.MaxValue };

        [Test]
        public void Events_AreSpacedBetweenNinetyAndOneHundredFiftySeconds()
        {
            foreach (int seed in Seeds)
            {
                for (int index = 0; index < 60; index++)
                {
                    double gap = CosmicEventRules.Get(seed, index + 1).StartTime - CosmicEventRules.Get(seed, index).StartTime;

                    Assert.That(gap, Is.InRange(90.0, 150.0), $"seed {seed} index {index}");
                }
            }
        }

        [Test]
        public void Events_NeverOverlapTheirWarningWindow()
        {
            foreach (int seed in Seeds)
            {
                for (int index = 0; index < 60; index++)
                {
                    CosmicEvent current = CosmicEventRules.Get(seed, index);
                    CosmicEvent next = CosmicEventRules.Get(seed, index + 1);

                    Assert.That(current.EndTime, Is.LessThan(next.WarningTime));
                }
            }
        }

        [Test]
        public void Events_FirstOneComesAfterTheWarmUp()
        {
            foreach (int seed in Seeds)
            {
                Assert.That(CosmicEventRules.Get(seed, 0).WarningTime, Is.GreaterThan(30.0));
            }
        }

        [Test]
        public void Events_EveryThreeInARowCoverAllTypes()
        {
            foreach (int seed in Seeds)
            {
                for (int block = 0; block < 20; block++)
                {
                    var types = new System.Collections.Generic.HashSet<CosmicEventType>
                    {
                        CosmicEventRules.TypeFor(seed, block * 3),
                        CosmicEventRules.TypeFor(seed, block * 3 + 1),
                        CosmicEventRules.TypeFor(seed, block * 3 + 2)
                    };

                    Assert.That(types.Count, Is.EqualTo(3), $"seed {seed} block {block}");
                    Assert.That(types, Has.No.Member(CosmicEventType.None));
                }
            }
        }

        [Test]
        public void Events_AreDeterministicAndSeedDependent()
        {
            CosmicEvent first = CosmicEventRules.Get(5, 4);
            CosmicEvent same = CosmicEventRules.Get(5, 4);
            CosmicEvent other = CosmicEventRules.Get(6, 4);

            Assert.That(same.StartTime, Is.EqualTo(first.StartTime));
            Assert.That(same.Position, Is.EqualTo(first.Position));
            Assert.That(other.StartTime != first.StartTime || other.Position != first.Position, Is.True);
        }

        [Test]
        public void Events_StartInsideTheWorld()
        {
			Rect bounds = WorldBounds.Area;
            foreach (int seed in Seeds)
            {
                for (int index = 0; index < 60; index++)
                {
                    CosmicEvent worldEvent = CosmicEventRules.Get(seed, index);

                    Assert.That(bounds.Contains(worldEvent.Position), Is.True, $"{worldEvent.Type} {worldEvent.Position}");
                }
            }
        }

        [Test]
        public void Events_MeteorsMoveAlongAUnitDirection()
        {
            for (int index = 0; index < 60; index++)
            {
                CosmicEvent worldEvent = CosmicEventRules.Get(3, index);
                if (worldEvent.Type != CosmicEventType.MeteorShower)
                {
                    continue;
                }

                Assert.That(worldEvent.Direction.magnitude, Is.EqualTo(1f).Within(1e-4f));
                Assert.That(worldEvent.Speed, Is.EqualTo(CosmicEventRules.MeteorSpeed));
                Assert.That(worldEvent.DurationSeconds, Is.EqualTo(CosmicEventRules.MeteorSeconds));
            }
        }

        [Test]
        public void Events_BlackHoleCrossesTheMapAndStopsAtTheEnd()
        {
			Rect bounds = WorldBounds.Area;
            for (int index = 0; index < 60; index++)
            {
                CosmicEvent worldEvent = CosmicEventRules.Get(9, index);
                if (worldEvent.Type != CosmicEventType.BlackHole)
                {
                    continue;
                }

                Vector2 start = worldEvent.PositionAt(worldEvent.StartTime);
                Vector2 end = worldEvent.PositionAt(worldEvent.EndTime);
                Vector2 later = worldEvent.PositionAt(worldEvent.EndTime + 100.0);

                Assert.That(start, Is.EqualTo(worldEvent.Position));
                Assert.That((end - start).magnitude, Is.EqualTo(CosmicEventRules.BlackHoleSpeed * 40f).Within(0.01f));
                Assert.That(bounds.Contains(end), Is.True);
                Assert.That(later, Is.EqualTo(end));
                Assert.That(worldEvent.PositionAt(worldEvent.StartTime - 50.0), Is.EqualTo(worldEvent.Position));
            }
        }

        [Test]
        public void Events_ActiveWindowIncludesWarningAndExcludesGaps()
        {
            CosmicEvent worldEvent = CosmicEventRules.Get(11, 2);

            Assert.That(CosmicEventRules.TryFindActive(11, worldEvent.WarningTime + 0.01, out CosmicEvent warning), Is.True);
            Assert.That(warning.Index, Is.EqualTo(2));
            Assert.That(CosmicEventRules.TryFindActive(11, worldEvent.EndTime - 0.01, out CosmicEvent ending), Is.True);
            Assert.That(ending.Index, Is.EqualTo(2));
            Assert.That(CosmicEventRules.TryFindActive(11, worldEvent.EndTime + 5.0, out _), Is.False);
            Assert.That(CosmicEventRules.TryFindActive(11, 1.0, out _), Is.False);
        }

        [Test]
        public void Stars_AreSpreadAwayFromHomeAndInsideTheWorld()
        {
			Rect bounds = WorldBounds.Area;
            foreach (int seed in Seeds)
            {
                var stars = new Vector2[MapFeatureRules.StarCount];
                MapFeatureRules.Stars(seed, stars);

                for (int index = 0; index < stars.Length; index++)
                {
                    Assert.That(bounds.Contains(stars[index]), Is.True);
                    Assert.That(stars[index].magnitude, Is.GreaterThanOrEqualTo(MapFeatureRules.HomeClearRadius));
                    for (int other = index + 1; other < stars.Length; other++)
                    {
                        Assert.That((stars[index] - stars[other]).magnitude, Is.GreaterThan(1f));
                    }
                }
            }
        }

        [Test]
        public void Stars_AreDeterministic()
        {
            var first = new Vector2[MapFeatureRules.StarCount];
            var second = new Vector2[MapFeatureRules.StarCount];

            MapFeatureRules.Stars(123, first);
            MapFeatureRules.Stars(123, second);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Wormholes_AreFarApartAndInsideTheWorld()
        {
			Rect bounds = WorldBounds.Area;
            foreach (int seed in Seeds)
            {
                MapFeatureRules.Wormholes(seed, out Vector2 first, out Vector2 second);

                Assert.That(bounds.Contains(first) && bounds.Contains(second), Is.True);
                Assert.That((first - second).magnitude, Is.GreaterThanOrEqualTo(MapFeatureRules.WormholeSeparation - 0.01f));
                Assert.That(first.magnitude, Is.GreaterThanOrEqualTo(MapFeatureRules.HomeClearRadius));
            }
        }

        [Test]
        public void Nebulae_FitInsideTheWorld()
        {
			Rect bounds = WorldBounds.Area;
            foreach (int seed in Seeds)
            {
                var discs = new MapDisc[MapFeatureRules.NebulaCount];
                MapFeatureRules.Nebulae(seed, discs);

                foreach (MapDisc disc in discs)
                {
                    Assert.That(disc.Radius, Is.InRange(28f, 40f));
                    Assert.That(disc.Center.x - disc.Radius, Is.GreaterThanOrEqualTo(bounds.xMin - 0.01f));
                    Assert.That(disc.Center.x + disc.Radius, Is.LessThanOrEqualTo(bounds.xMax + 0.01f));
                    Assert.That(disc.Center.y - disc.Radius, Is.GreaterThanOrEqualTo(bounds.yMin - 0.01f));
                    Assert.That(disc.Center.y + disc.Radius, Is.LessThanOrEqualTo(bounds.yMax + 0.01f));
                }
            }
        }

        [Test]
        public void Nebulae_ContainmentChecksDistance()
        {
            var discs = new[] { new MapDisc(new Vector2(10f, 10f), 5f) };

            Assert.That(MapFeatureRules.InsideAny(discs, new Vector2(12f, 12f)), Is.True);
            Assert.That(MapFeatureRules.InsideAny(discs, new Vector2(20f, 10f)), Is.False);
            Assert.That(MapFeatureRules.InsideAny(null, Vector2.zero), Is.False);
        }

        [Test]
        public void Modifier_IsDeterministicAndCoversEveryKind()
        {
            var seen = new System.Collections.Generic.HashSet<DailyModifier>();
            for (int day = 20000; day < 20400; day++)
            {
                Assert.That(DailyModifierRules.ForDay(day), Is.EqualTo(DailyModifierRules.ForDay(day)));
                seen.Add(DailyModifierRules.ForDay(day));
            }

            Assert.That(seen.Count, Is.EqualTo(4));
        }

        [Test]
        public void Modifier_ScalesMatchTheirNames()
        {
            Assert.That(DailyModifierRules.GravityScale(DailyModifier.None), Is.EqualTo(1f));
            Assert.That(DailyModifierRules.GravityScale(DailyModifier.LowGravity), Is.LessThan(1f));
            Assert.That(DailyModifierRules.GravityScale(DailyModifier.HeavyGravity), Is.GreaterThan(1f));
            Assert.That(DailyModifierRules.LootScale(DailyModifier.GoldRush), Is.GreaterThan(1f));
            Assert.That(DailyModifierRules.LootScale(DailyModifier.LowGravity), Is.EqualTo(1f));
        }

        [Test]
        public void Seed_DiffersBetweenDays()
        {
            Assert.That(WorldSeedRules.FromDay(20000), Is.Not.EqualTo(WorldSeedRules.FromDay(20001)));
            Assert.That(WorldSeedRules.FromDay(20000), Is.EqualTo(WorldSeedRules.FromDay(20000)));
        }
    }
}
