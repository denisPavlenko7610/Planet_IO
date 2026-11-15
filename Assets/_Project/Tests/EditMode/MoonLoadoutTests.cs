using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class MoonLoadoutTests
    {
        [Test]
        public void NewLoadout_HasNoMoons()
        {
            var loadout = new MoonLoadout();

            Assert.That(loadout.Slots, Is.EqualTo(0));
            Assert.That(loadout.AliveMask, Is.EqualTo(0));
            Assert.That(loadout.IsAlive(0), Is.False);
        }

        [Test]
        public void SetSlots_CreatesFullHealthMoons()
        {
            var loadout = new MoonLoadout();

            bool changed = loadout.SetSlots(4);

            Assert.That(changed, Is.True);
            Assert.That(loadout.AliveMask, Is.EqualTo(0b1111));
            Assert.That(loadout.SetSlots(4), Is.False);
        }

        [Test]
        public void SetSlots_ShrinkingRemovesHighestMoons()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(6);

            loadout.SetSlots(2);

            Assert.That(loadout.AliveMask, Is.EqualTo(0b11));
            Assert.That(loadout.IsAlive(2), Is.False);
        }

        [Test]
        public void SetSlots_GrowingKeepsDamagedMoonsBroken()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(2);
            BreakMoon(loadout, 0);

            loadout.SetSlots(4);

            Assert.That(loadout.IsAlive(0), Is.False);
            Assert.That(loadout.IsAlive(1), Is.True);
            Assert.That(loadout.IsAlive(2), Is.True);
            Assert.That(loadout.IsAlive(3), Is.True);
        }

        [Test]
        public void SetSlots_ClampsToMaximum()
        {
            var loadout = new MoonLoadout();

            loadout.SetSlots(99);

            Assert.That(loadout.Slots, Is.EqualTo(MoonLoadout.MaxSlots));
            loadout.SetSlots(-3);
            Assert.That(loadout.Slots, Is.EqualTo(0));
        }

        [Test]
        public void Hit_BreaksMoonOnlyAfterFullDurability()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(2);

            for (int hit = 1; hit < MoonRules.MaxDurability; hit++)
            {
                Assert.That(loadout.Hit(0), Is.False);
                Assert.That(loadout.IsAlive(0), Is.True);
            }

            Assert.That(loadout.Hit(0), Is.True);
            Assert.That(loadout.IsAlive(0), Is.False);
        }

        [Test]
        public void Hit_IgnoresMissingMoons()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(2);

            Assert.That(loadout.Hit(5), Is.False);
            Assert.That(loadout.Hit(-1), Is.False);
            BreakMoon(loadout, 1);
            Assert.That(loadout.Hit(1), Is.False);
        }

        [Test]
        public void Tick_RegrowsBrokenMoonAfterDelay()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(2);
            BreakMoon(loadout, 0);

            Assert.That(loadout.Tick(MoonRules.RegrowSeconds - 0.5f), Is.False);
            Assert.That(loadout.IsAlive(0), Is.False);
            Assert.That(loadout.Tick(0.6f), Is.True);
            Assert.That(loadout.IsAlive(0), Is.True);
        }

        [Test]
        public void Tick_RegrownMoonHasFullDurabilityAgain()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(1);
            BreakMoon(loadout, 0);
            loadout.Tick(MoonRules.RegrowSeconds + 1f);

            for (int hit = 1; hit < MoonRules.MaxDurability; hit++)
            {
                Assert.That(loadout.Hit(0), Is.False);
            }

            Assert.That(loadout.Hit(0), Is.True);
        }

        [Test]
        public void Reset_ClearsEverything()
        {
            var loadout = new MoonLoadout();
            loadout.SetSlots(4);
            BreakMoon(loadout, 1);

            loadout.Reset();

            Assert.That(loadout.Slots, Is.EqualTo(0));
            Assert.That(loadout.AliveMask, Is.EqualTo(0));
            Assert.That(loadout.Tick(100f), Is.False);
        }

        private static void BreakMoon(MoonLoadout loadout, int index)
        {
            for (int hit = 0; hit < MoonRules.MaxDurability; hit++)
            {
                loadout.Hit(index);
            }
        }
    }
}
