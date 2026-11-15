using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class GravityAndPerkRulesTests
    {
        [Test]
        public void Gravity_OnlyAffectsMuchSmallerBodies()
        {
            Assert.That(GravityRules.Affects(1f, 0.6f), Is.True);
            Assert.That(GravityRules.Affects(1f, 0.61f), Is.False);
            Assert.That(GravityRules.Affects(1f, 1f), Is.False);
            Assert.That(GravityRules.Affects(1f, 2f), Is.False);
        }

        [Test]
        public void Gravity_ZoneScalesWithRadiusAndPerk()
        {
            Assert.That(GravityRules.ZoneRadius(2f, 1f), Is.EqualTo(6f).Within(1e-5f));
            Assert.That(GravityRules.ZoneRadius(2f, 1.4f), Is.EqualTo(8.4f).Within(1e-5f));
            Assert.That(GravityRules.ZoneRadius(2f, 0.2f), Is.EqualTo(6f).Within(1e-5f));
        }

        [Test]
        public void Gravity_PullIsStrongestAtSurfaceAndZeroOutsideZone()
        {
            float surface = GravityRules.PullSpeed(2f, 2f, 6f);
            float middle = GravityRules.PullSpeed(4f, 2f, 6f);
            float edge = GravityRules.PullSpeed(6f, 2f, 6f);

            Assert.That(surface, Is.EqualTo(GravityRules.MaxPullSpeed).Within(1e-5f));
            Assert.That(middle, Is.LessThan(surface).And.GreaterThan(0f));
            Assert.That(edge, Is.EqualTo(0f));
            Assert.That(GravityRules.PullSpeed(10f, 2f, 6f), Is.EqualTo(0f));
        }

        [Test]
        public void Gravity_PullInsideBodyIsCappedAtMaximum()
        {
            Assert.That(GravityRules.PullSpeed(0.5f, 2f, 6f), Is.EqualTo(GravityRules.MaxPullSpeed).Within(1e-5f));
        }

        [Test]
        public void Gravity_DegenerateZoneGivesNoPull()
        {
            Assert.That(GravityRules.PullSpeed(1f, 2f, 2f), Is.EqualTo(0f));
        }

        [TestCase(0f, 1f, false)]
        [TestCase(90f, 1f, true)]
        [TestCase(61f, 1f, true)]
        [TestCase(119f, 1f, true)]
        [TestCase(59f, 1f, false)]
        [TestCase(121f, 1f, false)]
        [TestCase(30f, 1f, false)]
        [TestCase(150f, 1f, false)]
        [TestCase(90f, 0.25f, false)]
        public void Gravity_TangentialMotionNeedsAngleAndSpeed(float degrees, float speed, bool expected)
        {
            Vector2 towardWell = Vector2.right;
            Vector2 velocity = Quaternion.Euler(0f, 0f, degrees) * Vector2.right * speed * 2f;

            Assert.That(GravityRules.IsTangential(velocity, towardWell), Is.EqualTo(expected));
        }

        [Test]
        public void Slingshot_TriggersAfterSustainedTangentialMotion()
        {
            var state = new SlingshotState();

            Assert.That(state.Tick(0.3f, true), Is.False);
            Assert.That(state.Boost, Is.EqualTo(0f));
            Assert.That(state.Tick(0.3f, true), Is.True);
            Assert.That(state.Boost, Is.EqualTo(SlingshotState.BoostFraction).Within(1e-5f));
        }

        [Test]
        public void Slingshot_BoostFadesOverTime()
        {
            var state = new SlingshotState();
            state.Tick(0.6f, true);

            state.Tick(SlingshotState.BoostSeconds * 0.5f, false);

            Assert.That(state.Boost, Is.EqualTo(SlingshotState.BoostFraction * 0.5f).Within(1e-3f));
            state.Tick(SlingshotState.BoostSeconds, false);
            Assert.That(state.Boost, Is.EqualTo(0f));
        }

        [Test]
        public void Slingshot_BreakingTangentMotionResetsProgress()
        {
            var state = new SlingshotState();

            state.Tick(0.4f, true);
            state.Tick(1f, false);

            Assert.That(state.Tick(0.4f, true), Is.False);
        }

        [Test]
        public void Slingshot_RetriggerWhileActiveRefreshesWithoutReportingNewStart()
        {
            var state = new SlingshotState();
            Assert.That(state.Tick(0.6f, true), Is.True);

            Assert.That(state.Tick(0.6f, true), Is.False);
            Assert.That(state.Boost, Is.GreaterThan(SlingshotState.BoostFraction * 0.9f));
        }

        [Test]
        public void Slingshot_ResetClearsEverything()
        {
            var state = new SlingshotState();
            state.Tick(0.6f, true);

            state.Reset();

            Assert.That(state.Boost, Is.EqualTo(0f));
        }

        [Test]
        public void Perks_MaskHelpersTrackIndividualBits()
        {
            ushort mask = 0;
            mask = PerkRules.With(mask, PerkId.SolarSail);
            mask = PerkRules.With(mask, PerkId.Predator);

            Assert.That(PerkRules.Has(mask, PerkId.SolarSail), Is.True);
            Assert.That(PerkRules.Has(mask, PerkId.Predator), Is.True);
            Assert.That(PerkRules.Has(mask, PerkId.IonTrail), Is.False);
            Assert.That(PerkRules.OwnedCount(mask), Is.EqualTo(2));
        }

        [Test]
        public void Perks_EveryPerkFitsTheMask()
        {
            Assert.That(System.Enum.GetValues(typeof(PerkId)).Length, Is.EqualTo(PerkRules.Count));
            Assert.That(PerkRules.Count, Is.LessThanOrEqualTo(16));
        }

        [Test]
        public void Perks_NoPerksMeansNeutralMultipliers()
        {
            Assert.That(PerkRules.SpeedMultiplier(0), Is.EqualTo(1f));
            Assert.That(PerkRules.FoodMultiplier(0), Is.EqualTo(1f));
            Assert.That(PerkRules.MagnetRadiusMultiplier(0), Is.EqualTo(1f));
            Assert.That(PerkRules.MoonSlotBonus(0), Is.EqualTo(0));
            Assert.That(PerkRules.MoonDamageMultiplier(0), Is.EqualTo(1f));
            Assert.That(PerkRules.GravityZoneMultiplier(0), Is.EqualTo(1f));
            Assert.That(PerkRules.AbsorbMultiplier(0), Is.EqualTo(1f));
            Assert.That(PerkRules.CometHealFraction(0), Is.EqualTo(0f));
            Assert.That(PerkRules.BoostCostMultiplier(0), Is.EqualTo(1f));
        }

        [Test]
        public void Perks_EffectsMatchDesign()
        {
            Assert.That(PerkRules.MagnetRadiusMultiplier(PerkRules.Bit(PerkId.Magnetosphere)), Is.EqualTo(1.5f));
            Assert.That(PerkRules.SpeedMultiplier(PerkRules.Bit(PerkId.SolarSail)), Is.EqualTo(1.12f).Within(1e-5f));
            Assert.That(PerkRules.FoodMultiplier(PerkRules.Bit(PerkId.SolarSail)), Is.EqualTo(0.9f).Within(1e-5f));
            Assert.That(PerkRules.MoonSlotBonus(PerkRules.Bit(PerkId.HeavyMoons)), Is.EqualTo(1));
            Assert.That(PerkRules.MoonDamageMultiplier(PerkRules.Bit(PerkId.HeavyMoons)), Is.EqualTo(1.5f));
            Assert.That(PerkRules.GravityZoneMultiplier(PerkRules.Bit(PerkId.GravityWell)), Is.EqualTo(1.4f));
            Assert.That(PerkRules.AbsorbMultiplier(PerkRules.Bit(PerkId.Predator)), Is.EqualTo(1.2f));
            Assert.That(PerkRules.CometHealFraction(PerkRules.Bit(PerkId.CometRider)), Is.EqualTo(0.5f));
            Assert.That(PerkRules.BoostCostMultiplier(PerkRules.Bit(PerkId.IonTrail)), Is.EqualTo(1.25f));
        }

        [Test]
        public void Perks_EveryPerkHasADownsideOrSpecialCost()
        {
            ushort[] costly =
            {
                PerkRules.Bit(PerkId.Magnetosphere),
                PerkRules.Bit(PerkId.HeavyMoons),
                PerkRules.Bit(PerkId.SolarSail),
                PerkRules.Bit(PerkId.GravityWell),
                PerkRules.Bit(PerkId.Predator),
                PerkRules.Bit(PerkId.IonTrail)
            };

            foreach (ushort mask in costly)
            {
                bool slower = PerkRules.SpeedMultiplier(mask) < 1f;
                bool poorerFood = PerkRules.FoodMultiplier(mask) < 1f;
                bool costlierBoost = PerkRules.BoostCostMultiplier(mask) > 1f;
                bool fasterButPoorer = PerkRules.SpeedMultiplier(mask) > 1f && poorerFood;
                Assert.That(slower || poorerFood || costlierBoost || fasterButPoorer, Is.True, mask.ToString());
            }
        }

        [Test]
        public void Offer_ReturnsThreeDistinctValidPerks()
        {
            var offer = new byte[PerkRules.OfferSize];

            int count = PerkRules.FillOffer(0, 42, offer);

            Assert.That(count, Is.EqualTo(PerkRules.OfferSize));
            Assert.That(offer, Is.Unique);
            foreach (byte id in offer)
            {
                Assert.That(PerkRules.IsValid(id), Is.True);
            }
        }

        [Test]
        public void Offer_NeverContainsOwnedPerks()
        {
            ushort owned = PerkRules.With(PerkRules.With(0, PerkId.IonTrail), PerkId.Predator);
            var offer = new byte[PerkRules.OfferSize];

            for (int seed = 0; seed < 100; seed++)
            {
                int count = PerkRules.FillOffer(owned, seed, offer);

                Assert.That(count, Is.EqualTo(PerkRules.OfferSize));
                foreach (byte id in offer)
                {
                    Assert.That(PerkRules.Has(owned, (PerkId)id), Is.False);
                }
            }
        }

        [Test]
        public void Offer_ShrinksWhenFewPerksRemain()
        {
            ushort owned = 0;
            for (int id = 0; id < PerkRules.Count - 2; id++)
            {
                owned = PerkRules.With(owned, (PerkId)id);
            }

            var offer = new byte[PerkRules.OfferSize];

            Assert.That(PerkRules.FillOffer(owned, 1, offer), Is.EqualTo(2));
            Assert.That(PerkRules.FillOffer(ushort.MaxValue, 1, offer), Is.EqualTo(0));
        }

        [Test]
        public void Offer_IsDeterministicForSeed()
        {
            var first = new byte[PerkRules.OfferSize];
            var second = new byte[PerkRules.OfferSize];

            PerkRules.FillOffer(0, 7, first);
            PerkRules.FillOffer(0, 7, second);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Offer_CoversEveryPerkAcrossSeeds()
        {
            var seen = new bool[PerkRules.Count];
            var offer = new byte[PerkRules.OfferSize];

            for (int seed = 0; seed < 200; seed++)
            {
                PerkRules.FillOffer(0, seed, offer);
                foreach (byte id in offer)
                {
                    seen[id] = true;
                }
            }

            Assert.That(seen, Is.All.True);
        }
    }
}
