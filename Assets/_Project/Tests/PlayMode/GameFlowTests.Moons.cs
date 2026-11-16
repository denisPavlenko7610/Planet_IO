using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_ServerPublishesSlotsWhenCrossingTiers()
        {
            yield return StartMoonGame();
            Player player = FindLocalPlayer();
            foreach (var entry in new[] { (0.1f, 1, 0), (0.35f, 2, 3), (0.8f, 3, 15), (1.4f, 4, 63), (0.1f, 1, 0) })
            {
                player.Capacity = entry.Item1;
                Assert.That(player.Tier, Is.EqualTo(entry.Item2));
                Assert.That(player.MoonMask, Is.EqualTo(entry.Item3));
            }
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_StrikeTransfersHalfDamageAndImmunityPreventsSecondHit()
        {
            yield return StartMoonGame();
            Player attacker = PrepareMoonPlayer();
            Enemy victim = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            victim.Capacity = 1f;
            PlaceAtMoon(attacker, victim.transform);

            StepMoons(attacker);

            Assert.That(victim.Capacity, Is.EqualTo(0.94f).Within(1e-5f), "GDD: 6% damage.");
            Assert.That(attacker.Capacity, Is.EqualTo(0.43f).Within(1e-5f), "GDD: owner gains 50% of damage.");
            PlaceAtMoon(attacker, victim.transform);
            StepMoons(attacker);
            Assert.That(victim.Capacity, Is.EqualTo(0.94f).Within(1e-5f), "GDD: one second of immunity to all moons.");
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_SpawnProtectedOwnerCannotStrike()
        {
            yield return StartMoonGame();
            Player attacker = PrepareMoonPlayer();
            SetMoonField(attacker, "_invincibilityTimeRemaining", 1f);
            Enemy victim = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            victim.Capacity = 1f;
            PlaceAtMoon(attacker, victim.transform);

            StepMoons(attacker);

            Assert.That(victim.Capacity, Is.EqualTo(1f));
            Assert.That(attacker.Capacity, Is.EqualTo(0.4f));
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_SpawnProtectedVictimCannotBeDamaged()
        {
            yield return StartMoonGame();
            Player victim = PrepareMoonPlayer();
            SetMoonField(victim, "_invincibilityTimeRemaining", 1f);
            Enemy attacker = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            attacker.Capacity = 1f;
            attacker.transform.position = new Vector2(-40f, -40f);
            PlaceAtMoon(attacker, victim.transform);

            StepMoons(attacker);

            Assert.That(victim.Capacity, Is.EqualTo(0.4f));
            Assert.That(attacker.Capacity, Is.EqualTo(1f));
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_InterceptCometsAndBreakAfterThreeHits()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
            for (int hit = 0; hit < 3; hit++)
            {
                Vector2 contact = PlaceAtMoon(player, comet.transform);
                StepMoons(player);
                Assert.That(Vector2.Distance(comet.transform.position, contact), Is.GreaterThan(0.1f), "Intercepted comet must respawn.");
                Assert.That(player.Capacity, Is.EqualTo(0.4f), "Intercept must not damage the owner.");
                Assert.That(player.MoonMask, Is.EqualTo(hit < 2 ? 3 : 2), "Moon breaks on the third hit.");
            }
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_DefeatClearsSatellites()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            Assert.That(player.MoonMask, Is.EqualTo(3));

            player.Defeat();

            Assert.That(player.IsDefeated, Is.True);
            Assert.That(player.MoonMask, Is.Zero, "GDD 6.2: satellites reset on death.");
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Moons_ContinueDoesNotRetainBrokenMoonFromPreviousLife()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 1f;
            MoonLoadout loadout = (MoonLoadout)typeof(PlanetScale).GetField("_moons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            for (int hit = 0; hit < 3; hit++) loadout.Hit(0);
            typeof(PlanetScale).GetMethod("PublishMoonMask", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            Assert.That(player.MoonMask, Is.EqualTo(14));

            player.Defeat();
            player.ContinueRpc();

            Assert.That(player.IsDefeated, Is.False);
            Assert.That(player.Capacity, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(player.Tier, Is.EqualTo(2));
            Assert.That(player.MoonMask, Is.EqualTo(3), "GDD 6.2: death resets satellites; continue retains perks and restores 50% mass.");
        }

        private static IEnumerator StartMoonGame()
        {
            yield return StartSoloGame();
            yield return WaitForGameplay(8f);
        }

        private static Player PrepareMoonPlayer()
        {
            Player player = FindLocalPlayer();
            Assert.That(player, Is.Not.Null);
            player.Capacity = 0.4f;
            player.transform.position = new Vector2(-40f, -40f);
            SetMoonField(player, "_invincibilityTimeRemaining", 0f);
            return player;
        }

        private static Vector2 PlaceAtMoon(PlanetScale owner, Transform target)
        {
            Vector2 position = MoonRules.Position(owner.transform.position, owner.BodyRadius, 0,
                TierRules.MoonSlots(owner.Tier), OrbitClock.Now, owner.MoonPhaseDegrees);
            target.position = position;
            Physics2D.SyncTransforms();
            return position;
        }

        private static void StepMoons(PlanetScale owner)
        {
            SetMoonField(owner, "_moonStepTimer", 0f);
            typeof(PlanetScale).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
        }

        private static void SetMoonField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? typeof(PlanetScale).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
