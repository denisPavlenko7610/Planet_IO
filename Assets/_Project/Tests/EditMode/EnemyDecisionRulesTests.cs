using NUnit.Framework;
using PlanetIO;

namespace PlanetIO.Tests
{
    public sealed class EnemyDecisionRulesTests
    {
        [Test]
        public void Hazard_AlwaysHasHighestPriority()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                2f,
                0.5f,
                true,
                true,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Evade));
        }

        [Test]
        public void BiggerOpponent_HuntsPlayer()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1.5f,
                1f,
                true,
                false,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Hunt));
        }

        [Test]
        public void SmallerOpponent_EvadesPlayer()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f,
                1.5f,
                true,
                false,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Evade));
        }

        [Test]
        public void Food_IsSelectedWhenPlayerIsNotActionable()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f,
                0.98f,
                true,
                false,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Forage));
        }

        [Test]
        public void CapacitySpeedMultiplier_NeverFallsBelowMinimum()
        {
            float multiplier =
                EnemyDecisionRules.GetCapacitySpeedMultiplier(
                    100f,
                    0.08f,
                    0.3f,
                    0.55f);

            Assert.That(multiplier, Is.EqualTo(0.55f).Within(0.0001f));
        }
    }
}
