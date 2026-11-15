using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class CombatRulesTests
    {
        [Test]
        public void CanDevour_RequiresRatioMargin()
        {
            Assert.That(CombatRules.CanDevour(1.08f, 1f, 1.08f), Is.True);
            Assert.That(CombatRules.CanDevour(1.07f, 1f, 1.08f), Is.False);
        }

        [Test]
        public void CanDevour_EqualSizesNeverFightWhenRatioAboveOne()
        {
            Assert.That(CombatRules.CanDevour(0.5f, 0.5f, CombatRules.DefaultEatSizeRatio), Is.False);
        }

        [Test]
        public void CanDevour_RatioBelowOneIsClampedToOne()
        {
            Assert.That(CombatRules.CanDevour(0.9f, 1f, 0.2f), Is.False);
            Assert.That(CombatRules.CanDevour(1f, 1f, 0.2f), Is.True);
        }

        [Test]
        public void CanDevour_NaNNeverDevours()
        {
            Assert.That(CombatRules.CanDevour(float.NaN, 1f, 1.08f), Is.False);
            Assert.That(CombatRules.CanDevour(1f, float.NaN, 1.08f), Is.False);
        }

        [Test]
        public void AbsorbedMass_ScalesByFraction()
        {
            Assert.That(CombatRules.AbsorbedMass(1f, 0.4f), Is.EqualTo(0.4f).Within(1e-6f));
        }

        [Test]
        public void AbsorbedMass_ClampsNegativeInputsAndFraction()
        {
            Assert.That(CombatRules.AbsorbedMass(-1f, 0.4f), Is.EqualTo(0f));
            Assert.That(CombatRules.AbsorbedMass(1f, 2f), Is.EqualTo(1f));
            Assert.That(CombatRules.AbsorbedMass(1f, -1f), Is.EqualTo(0f));
        }

        [Test]
        public void PlanetScale_CanDevourUsesSerializedRatio()
        {
            Enemy big = CreateEnemy("Big");
            Enemy small = CreateEnemy("Small");
            try
            {
                big.Capacity = 1f;
                small.Capacity = 0.5f;

                Assert.That(big.CanDevour(small), Is.True);
                Assert.That(small.CanDevour(big), Is.False);
                Assert.That(big.CanDevour(null), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(big.gameObject);
                UnityEngine.Object.DestroyImmediate(small.gameObject);
            }
        }

        private static Enemy CreateEnemy(string name)
        {
            var root = new UnityEngine.GameObject(name);
            root.AddComponent<UnityEngine.CircleCollider2D>();
            return root.AddComponent<Enemy>();
        }
    }
}
