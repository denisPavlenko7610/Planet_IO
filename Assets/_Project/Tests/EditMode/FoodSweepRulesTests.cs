using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class FoodSweepRulesTests
    {
        [Test]
        public void Advance_DoesNotFireBeforeTheInterval()
        {
            float timer = 0f;

            Assert.That(FoodSweepRules.Advance(ref timer, 0.02f), Is.False);
            Assert.That(FoodSweepRules.Advance(ref timer, 0.02f), Is.False);
            Assert.That(timer, Is.EqualTo(0.04f).Within(1e-6f));
        }

        [Test]
        public void Advance_FiresOncePerIntervalAndKeepsTheRemainder()
        {
            float timer = 0.09f;

            Assert.That(FoodSweepRules.Advance(ref timer, 0.02f), Is.True);
            Assert.That(timer, Is.EqualTo(0.01f).Within(1e-6f));
        }

        [Test]
        public void Advance_FiresOnlyOnceAfterALongHitch()
        {
            float timer = 0f;

            Assert.That(FoodSweepRules.Advance(ref timer, 0.35f), Is.True);
            Assert.That(timer, Is.LessThan(FoodSweepRules.Interval));
            Assert.That(FoodSweepRules.Advance(ref timer, 0f), Is.False);
        }

        [Test]
        public void Advance_FiresAboutTenTimesPerSecondAtTheFixedStep()
        {
            float timer = 0f;
            int fired = 0;

            for (int step = 0; step < 50; step++)
            {
                if (FoodSweepRules.Advance(ref timer, 0.02f))
                {
                    fired++;
                }
            }

            Assert.That(fired, Is.InRange(9, 10));
        }
    }
}
