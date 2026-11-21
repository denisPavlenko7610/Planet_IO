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
        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_ServerPublishesSlotsWhenCrossingTiers()
        {
            yield return StartSharedMoonGame();
            Player player = FindLocalPlayer();
            foreach (var entry in new[] { (0.1f, 1, 0), (0.35f, 2, 3), (0.8f, 3, 15), (1.4f, 4, 63), (0.1f, 1, 0) })
            {
                player.Capacity = entry.Item1;
                Assert.That(player.Tier, Is.EqualTo(entry.Item2));
                Assert.That(player.MoonMask, Is.EqualTo(entry.Item3));
            }
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_StrikeTransfersHalfDamageAndImmunityPreventsSecondHit()
        {
            yield return StartSharedMoonGame();
            Player attacker = PrepareMoonPlayer();
            Enemy victim = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            victim.Capacity = 1f;
            PlaceAtMoon(attacker, victim.transform);

            StepMoons(attacker);

            Assert.That(victim.Capacity, Is.EqualTo(0.9f).Within(1e-5f), "GDD: 10% damage.");
            Assert.That(attacker.Capacity, Is.EqualTo(0.45f).Within(1e-5f), "GDD: owner gains 50% of damage.");
            PlaceAtMoon(attacker, victim.transform);
            StepMoons(attacker);
            Assert.That(victim.Capacity, Is.EqualTo(0.9f).Within(1e-5f), "GDD: one second of immunity to all moons.");
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_SpawnProtectedOwnerCannotStrike()
        {
            yield return StartSharedMoonGame();
            Player attacker = PrepareMoonPlayer();
            SetMoonField(attacker, "_invincibilityTimeRemaining", 1f);
            Enemy victim = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            victim.Capacity = 1f;
            PlaceAtMoon(attacker, victim.transform);

            StepMoons(attacker);

            Assert.That(victim.Capacity, Is.EqualTo(1f));
            Assert.That(attacker.Capacity, Is.EqualTo(0.4f));
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_SpawnProtectedVictimCannotBeDamaged()
        {
            yield return StartSharedMoonGame();
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

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_CometDestroysTheHitMoonAtOnceAndSparesTheOwner()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
            Assert.That(player.MoonMask, Is.EqualTo(3));

            Vector2 contact = PlaceAtMoon(player, comet.transform);
            StepMoons(player);

            Assert.That(Vector2.Distance(comet.transform.position, contact), Is.GreaterThan(0.1f), "Intercepted comet must respawn.");
            Assert.That(player.Capacity, Is.EqualTo(0.4f), "Intercept must not damage the owner.");
            Assert.That(player.MoonMask, Is.EqualTo(2), "Only the hit moon disappears, regardless of its durability.");
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_DefeatClearsSatellites()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            Assert.That(player.MoonMask, Is.EqualTo(3));

            player.Defeat();

            Assert.That(player.IsDefeated, Is.True);
            Assert.That(player.MoonMask, Is.Zero, "GDD 6.2: satellites reset on death.");
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Moons_ContinueDoesNotRetainBrokenMoonFromPreviousLife()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 1f;
            MoonLoadout loadout = (MoonLoadout)typeof(PlanetScale).GetField("_moons", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
            for (int hit = 0; hit < MoonRules.MaxDurability; hit++) loadout.Hit(0);
            typeof(PlanetScale).GetMethod("PublishMoonMask", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(player, null);
            Assert.That(player.MoonMask, Is.EqualTo(14));

            player.Defeat();
            player.ContinueRpc();

            Assert.That(player.IsDefeated, Is.False);
            Assert.That(player.Capacity, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(player.Tier, Is.EqualTo(2));
            Assert.That(player.MoonMask, Is.EqualTo(3), "GDD 6.2: death resets satellites; continue retains perks and restores 50% mass.");
        }

		[UnityTest, SharedWorld, Timeout(45000)]
		public IEnumerator Moons_ViewHidesOutsideCameraAndReturnsWithoutStalePositions()
		{
			yield return StartSharedMoonGame();
			Player player = PrepareMoonPlayer();
			MoonRingView view = player.GetComponent<MoonRingView>();
			MethodInfo refresh = typeof(MoonRingView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
			try
			{
				ViewBounds.Clear();
				refresh.Invoke(view, null);
				Transform root = (Transform)typeof(MoonRingView).GetField("_root", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
				Assert.That(root, Is.Not.Null);
				Assert.That(root.GetComponentsInChildren<SpriteRenderer>().Length, Is.GreaterThan(0));

				ViewBounds.Set(new Rect(500f, 500f, 10f, 10f));
				refresh.Invoke(view, null);
				Assert.That(root.gameObject.activeInHierarchy, Is.False, "Offscreen moon renderers must leave the active hierarchy.");

				player.transform.position += Vector3.right * 10f;
				ViewBounds.Clear();
				refresh.Invoke(view, null);
				SpriteRenderer[] moons = root.GetComponentsInChildren<SpriteRenderer>().Where(moon => moon.enabled).ToArray();
				Assert.That(moons.Length, Is.GreaterThan(0));
				foreach (SpriteRenderer moon in moons)
				{
					Assert.That(Vector2.Distance(moon.transform.position, player.transform.position), Is.LessThanOrEqualTo(MoonRules.OuterExtent(player.BodyRadius)));
				}
			}
			finally
			{
				ViewBounds.Clear();
			}
		}

        private static IEnumerator StartMoonGame(bool dailyWorld = false)
        {
            yield return StartSoloGame();
            yield return WaitForGameplay(8f);
            if (!dailyWorld)
            {
                ((System.IDisposable)ResolveGame<IWorldEvents>()).Dispose();
            }
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
