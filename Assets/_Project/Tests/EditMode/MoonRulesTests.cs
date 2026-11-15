using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class MoonRulesTests
    {
        [TestCase(0.08f, 1, 1)]
        [TestCase(0.34f, 1, 1)]
        [TestCase(0.35f, 1, 2)]
        [TestCase(0.8f, 1, 3)]
        [TestCase(1.4f, 1, 4)]
        [TestCase(2f, 1, 4)]
        [TestCase(2f, 99, 4)]
        [TestCase(0f, -5, 1)]
        public void Tier_ResolvesFromCapacity(float capacity, int currentTier, int expected)
        {
            Assert.That(TierRules.Resolve(capacity, currentTier), Is.EqualTo(expected));
        }

        [Test]
        public void Tier_HasHysteresisWhenShrinking()
        {
            Assert.That(TierRules.Resolve(0.34f, 2), Is.EqualTo(2));
            Assert.That(TierRules.Resolve(0.33f, 2), Is.EqualTo(2));
            Assert.That(TierRules.Resolve(0.3f, 2), Is.EqualTo(1));
        }

        [Test]
        public void Tier_CanDropSeveralTiersAtOnce()
        {
            Assert.That(TierRules.Resolve(0.1f, 4), Is.EqualTo(1));
        }

        [Test]
        public void Tier_NaNKeepsCurrentTier()
        {
            Assert.That(TierRules.Resolve(float.NaN, 3), Is.EqualTo(3));
        }

        [TestCase(1, 0)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        [TestCase(4, 6)]
        public void Tier_MoonSlots(int tier, int expected)
        {
            Assert.That(TierRules.MoonSlots(tier), Is.EqualTo(expected));
            Assert.That(expected, Is.LessThanOrEqualTo(MoonLoadout.MaxSlots));
        }

        [TestCase(0f, 1, 0f)]
        [TestCase(0.175f, 1, 0.5f)]
        [TestCase(0.35f, 1, 1f)]
        [TestCase(0.575f, 2, 0.5f)]
        [TestCase(1.1f, 3, 0.5f)]
        [TestCase(5f, 4, 1f)]
        [TestCase(-1f, 2, 0f)]
        public void Tier_ProgressIsFractionTowardNextTier(float capacity, int tier, float expected)
        {
            Assert.That(TierRules.Progress(capacity, tier), Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Tier_ProgressIgnoresNaN()
        {
            Assert.That(TierRules.Progress(float.NaN, 2), Is.EqualTo(0f));
        }

        [Test]
        public void OuterExtent_CoversOrbitAndMoon()
        {
            Assert.That(
                MoonRules.OuterExtent(2f),
                Is.EqualTo(2f * (MoonRules.OrbitRadiusFactor + MoonRules.MoonRadiusFactor)).Within(1e-5f));
            Assert.That(MoonRules.OuterExtent(1f), Is.GreaterThan(MoonRules.OrbitRadius(1f)));
        }

        [Test]
        public void Damage_IsPercentOfCapacityWithFloor()
        {
            Assert.That(MoonRules.Damage(1f), Is.EqualTo(0.06f).Within(1e-6f));
            Assert.That(MoonRules.Damage(0.05f), Is.EqualTo(MoonRules.MinHitDamage));
            Assert.That(MoonRules.Damage(0.08f), Is.EqualTo(0.0048f).Within(1e-6f));
        }

        [Test]
        public void Position_StaysOnOrbitRadius()
        {
            Vector2 center = new(3f, -2f);

            for (int index = 0; index < 6; index++)
            {
                Vector2 position = MoonRules.Position(center, 2f, index, 6, 12.5, 40f);

                Assert.That(Vector2.Distance(center, position), Is.EqualTo(MoonRules.OrbitRadius(2f)).Within(1e-4f));
            }
        }

        [Test]
        public void Position_SpreadsMoonsEvenly()
        {
            Vector2 first = MoonRules.Position(Vector2.zero, 1f, 0, 2, 0.0, 0f);
            Vector2 second = MoonRules.Position(Vector2.zero, 1f, 1, 2, 0.0, 0f);

            Assert.That((first + second).magnitude, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Position_RotatesWithTime()
        {
            Vector2 start = MoonRules.Position(Vector2.zero, 1f, 0, 4, 0.0, 0f);
            Vector2 later = MoonRules.Position(Vector2.zero, 1f, 0, 4, 1.0, 0f);

            Assert.That(Vector2.SignedAngle(start, later), Is.EqualTo(MoonRules.AngularSpeedDegrees).Within(1e-2f));
        }

        [Test]
        public void Position_IsStableForLargeServerTime()
        {
            Vector2 position = MoonRules.Position(Vector2.zero, 1f, 0, 4, 1.0e7, 0f);

            Assert.That(float.IsFinite(position.x) && float.IsFinite(position.y), Is.True);
            Assert.That(position.magnitude, Is.EqualTo(MoonRules.OrbitRadius(1f)).Within(1e-3f));
        }

        [Test]
        public void Position_ZeroSlotsDoesNotDivideByZero()
        {
            Vector2 position = MoonRules.Position(Vector2.zero, 1f, 0, 0, 0.0, 0f);

            Assert.That(float.IsFinite(position.x) && float.IsFinite(position.y), Is.True);
        }
    }
}
