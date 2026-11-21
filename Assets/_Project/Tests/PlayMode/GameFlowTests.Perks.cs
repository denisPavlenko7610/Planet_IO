using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_TierUpOffersThreeChoicesAndPickGrantsPerk()
        {
            yield return StartSharedMoonGame();
			Player player = PreparePerkPlayer();
            PerkId[] offered = null;
            player.PerkOfferShown += (perks, _) => offered = perks;

            player.Capacity = 0.4f;
            yield return WaitUntil(() => offered != null, 3f);

            Assert.That(offered, Is.Not.Null, "Reaching tier II must offer a perk.");
            Assert.That(offered.Length, Is.EqualTo(PerkRules.OfferSize));
            Assert.That(offered, Is.Unique);
            Assert.That(player.PerkMask, Is.Zero);

            player.ChoosePerk(1);
            yield return WaitUntil(() => player.PerkMask != 0, 3f);

            Assert.That(player.HasPerk(offered[1]), Is.True);
            Assert.That(PerkRules.OwnedCount(player.PerkMask), Is.EqualTo(1));
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_UnansweredOfferResolvesItselfAndClosesView()
        {
            yield return StartSharedMoonGame();
			Player player = PreparePerkPlayer();
            bool shown = false;
            bool closed = false;
            player.PerkOfferShown += (_, _) => shown = true;
            player.PerkOfferClosed += () => closed = true;

            player.Capacity = 0.4f;
            yield return WaitUntil(() => shown, 3f);
            SetMoonField(player, "_offerRemaining", 0.01f);
            yield return WaitUntil(() => closed, 3f);

            Assert.That(closed, Is.True);
            Assert.That(PerkRules.OwnedCount(player.PerkMask), Is.EqualTo(1), "Timeout picks one random perk.");
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_RegainingTierInSameLifeDoesNotOfferAgain()
        {
            yield return StartSharedMoonGame();
			Player player = PreparePerkPlayer();
            int offers = 0;
            player.PerkOfferShown += (_, _) => offers++;

            player.Capacity = 0.4f;
            yield return WaitUntil(() => offers == 1, 3f);
            player.ChoosePerk(0);
            yield return WaitUntil(() => player.PerkMask != 0, 3f);
            player.Capacity = 0.1f;
            player.Capacity = 0.4f;
            yield return new WaitForSeconds(0.5f);

            Assert.That(offers, Is.EqualTo(1));
            Assert.That(ReadPrivateField<int>(player, "_pendingOffers"), Is.Zero);
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_JumpingSeveralTiersQueuesOneOfferPerTier()
        {
            yield return StartSharedMoonGame();
			Player player = PreparePerkPlayer();

            player.Capacity = 1.5f;

            Assert.That(ReadPrivateField<int>(player, "_pendingOffers"), Is.EqualTo(3));
            yield return null;
        }

		private static Player PreparePerkPlayer()
		{
			Player player = PrepareMoonPlayer();
			player.Capacity = 0.1f;
			typeof(Player).GetMethod("ResetPerksForNewLife", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(player, null);
			return player;
		}

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_RespawnClearsPerksButContinueKeepsThem()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            GrantPerk(player, PerkId.SolarSail);
            player.Capacity = 1f;

            player.Defeat();
            player.ContinueRpc();

            Assert.That(player.HasPerk(PerkId.SolarSail), Is.True, "Continue keeps perks.");

            player.Defeat();
            player.RespawnRpc();

            Assert.That(player.PerkMask, Is.Zero, "A new life starts without perks.");
            Assert.That(player.MoonMask, Is.Zero);
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_HeavyMoonsAddASlotAndRemovingItDropsIt()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            Assert.That(player.MoonMask, Is.EqualTo(3));

            GrantPerk(player, PerkId.HeavyMoons);

            Assert.That(player.MoonSlots, Is.EqualTo(3));
            Assert.That(player.MoonMask, Is.EqualTo(7));
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_AblativeCrustSurvivesOneLethalHit()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            GrantPerk(player, PerkId.AblativeCrust);

            player.Capacity = player.MinCapacity;
            yield return null;
            yield return null;

            Assert.That(player.IsDefeated, Is.False);
            Assert.That(player.HasPerk(PerkId.AblativeCrust), Is.False, "The crust is consumed.");
            Assert.That(player.Capacity, Is.GreaterThanOrEqualTo(player.MinCapacity));
            Assert.That(player.IsSpawnProtected, Is.True);

            SetMoonField(player, "_invincibilityTimeRemaining", 0f);
            player.Capacity = 0.3f;
            player.Capacity = player.MinCapacity;

            Assert.That(player.IsDefeated, Is.True, "Second lethal hit is final.");
            yield return null;
        }

		[UnityTest, SharedWorld, Timeout(45000)]
		public IEnumerator Perks_AblativeCrustPreservesFoodEatenBeforeDeferredRestore()
		{
			yield return StartSharedMoonGame();
			Player player = PrepareMoonPlayer();
			GrantPerk(player, PerkId.AblativeCrust);
			player.Capacity = player.MinCapacity;
			Assert.That(player.HasPerk(PerkId.AblativeCrust), Is.False);

			FoodRecord food = AllFood().First(candidate => candidate.Active && !candidate.IsDropped);
			food.ResetClaim();
			food.SetNutrition(0.02f);
			Assert.That(player.TryEatFood(food), Is.True);
			float fedCapacity = player.Capacity;
			Assert.That(fedCapacity, Is.GreaterThan(player.MinCapacity));

			yield return null;
			yield return null;

			Assert.That(player.IsDefeated, Is.False, "Deferred healing must not kill a fed survivor.");
			Assert.That(player.Capacity, Is.GreaterThanOrEqualTo(fedCapacity), "Food gained before healing must remain.");
		}

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_AblativeCrustProtectsFromBeingEaten()
        {
            yield return StartSharedMoonGame();
            Player victim = PrepareMoonPlayer();
            victim.Capacity = 1f;
            GrantPerk(victim, PerkId.AblativeCrust);
            Enemy hunter = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            hunter.Capacity = 2f;
            hunter.transform.position = victim.transform.position;
            Physics2D.SyncTransforms();

            MethodInfo trigger = typeof(Enemy).GetMethod(
                "OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
            trigger.Invoke(hunter, new object[] { victim.GetComponent<Collider2D>() });

            Assert.That(victim.IsDefeated, Is.False);
            Assert.That(victim.HasPerk(PerkId.AblativeCrust), Is.False);
            Assert.That(hunter.Capacity, Is.EqualTo(2f), "A failed kill must not feed the hunter.");
            Assert.That(victim.Capacity, Is.EqualTo(0.3f).Within(1e-4f), "The survivor keeps 30% of its mass.");
            Assert.That(victim.IsSpawnProtected, Is.True);
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_CometRiderHealsInsteadOfHurting()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 1f;
            GrantPerk(player, PerkId.CometRider);
            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
            comet.transform.position = player.transform.position;
            Physics2D.SyncTransforms();
            float before = player.Capacity;

            TouchWith(player, comet.GetComponent<Collider2D>());

            Assert.That(player.Capacity, Is.EqualTo(before * 1.1f).Within(1e-5f));

			comet.transform.position = player.transform.position;
			Physics2D.SyncTransforms();
			float afterFirst = player.Capacity;
			TouchWith(player, comet.GetComponent<Collider2D>());

			Assert.That(player.Capacity, Is.EqualTo(afterFirst), "A second comet during the cooldown neither heals nor hurts.");
            yield return null;
        }

		[UnityTest, SharedWorld, Timeout(45000)]
		public IEnumerator Perks_AblativeCrustDropsTheLostMassAsLoot()
		{
			yield return StartSharedMoonGame();
			Player victim = PrepareMoonPlayer();
			victim.Capacity = 1f;
			GrantPerk(victim, PerkId.AblativeCrust);
			Enemy hunter = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
			hunter.Capacity = 2f;
			hunter.transform.position = victim.transform.position;
			Physics2D.SyncTransforms();
			float dropsBefore = SumDroppedNutrition();

			MethodInfo trigger = typeof(Enemy).GetMethod(
				"OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
			trigger.Invoke(hunter, new object[] { victim.GetComponent<Collider2D>() });

			Assert.That(SumDroppedNutrition() - dropsBefore, Is.EqualTo(0.7f).Within(0.01f), "The mass the crust removes must drop as loot.");
			yield return null;
		}

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_NebulaVeilHidesPlayerAfterBoostEnds()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            GrantPerk(player, PerkId.NebulaVeil);
            MethodInfo setBoosting = typeof(Player).GetMethod(
                "SetServerBoosting", BindingFlags.Instance | BindingFlags.NonPublic);

            setBoosting.Invoke(player, new object[] { true });
            setBoosting.Invoke(player, new object[] { false });
            yield return WaitUntil(() => player.IsStealthed, 3f);

            Assert.That(player.IsStealthed, Is.True);
            Assert.That(player.IsHiddenFromRivals, Is.True);

            setBoosting.Invoke(player, new object[] { true });
            setBoosting.Invoke(player, new object[] { false });
            SetMoonField(player, "_stealthRemaining", 0.01f);
            yield return WaitUntil(() => !player.IsStealthed, 3f);

            Assert.That(player.IsStealthed, Is.False, "Stealth ends and cools down for 40 s.");
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Perks_IonTrailDamagesRivalsThatTouchIt()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 0.4f;
            GrantPerk(player, PerkId.IonTrail);
            Enemy victim = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            victim.Capacity = 1f;
            victim.transform.position = (Vector2)player.transform.position + Vector2.right * 3f;
            Physics2D.SyncTransforms();

            Vector2[] positions = ReadPrivateField<Vector2[]>(player, "_trailPositions");
            float[] remaining = ReadPrivateField<float[]>(player, "_trailRemaining");
            positions[0] = victim.transform.position;
            remaining[0] = PerkRules.IonTrailPointSeconds;
            SetMoonField(player, "_trailStrikeTimer", 0f);
            MethodInfo update = typeof(Player).GetMethod(
                "UpdateIonTrail", BindingFlags.Instance | BindingFlags.NonPublic);

            update.Invoke(player, new object[] { 0.001f });

            Assert.That(victim.Capacity, Is.LessThan(1f));
            Assert.That(player.Capacity, Is.GreaterThan(0.4f), "Owner gains half the trail damage.");
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Gravity_PullsSmallBodyTowardMuchBiggerPlanet()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 0.3f;
            Enemy well = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            well.Capacity = 1.5f;
            well.transform.position = new Vector2(-40f, -40f);
            float distance = well.BodyRadius + (well.GravityZoneRadius - well.BodyRadius) * 0.5f;
            player.transform.position = (Vector2)well.transform.position + Vector2.right * distance;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            body.position = player.transform.position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            Assert.That(well.Pulls(player), Is.True);
            PlanetGravity gravity = player.GetComponent<PlanetGravity>();
            MethodInfo step = typeof(PlanetGravity).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            Vector2 before = body.position;

            step.Invoke(gravity, null);

            Vector2 moved = body.position - before;
            float expected = GravityRules.PullSpeed(distance, well.BodyRadius, well.GravityZoneRadius) * Time.fixedDeltaTime;
            Assert.That(moved.x, Is.LessThan(0f), "Pulled toward the well.");
            Assert.That(moved.magnitude, Is.EqualTo(expected).Within(expected * 0.2f + 1e-4f));
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Gravity_DoesNotPullComparableBodies()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 0.5f;
            Enemy peer = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            peer.Capacity = 0.7f;
            peer.transform.position = (Vector2)player.transform.position + Vector2.right * 3f;
            Physics2D.SyncTransforms();

            Assert.That(peer.Pulls(player), Is.False);
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Gravity_SlingshotRaisesSpeedMultiplier()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            PlanetGravity gravity = player.GetComponent<PlanetGravity>();
            Assert.That(player.SpeedMultiplier, Is.EqualTo(1f).Within(1e-5f));

            FieldInfo field = typeof(PlanetGravity).GetField("_slingshot", BindingFlags.Instance | BindingFlags.NonPublic);
            var state = (SlingshotState)field.GetValue(gravity);
            state.Tick(SlingshotState.TriggerSeconds + 0.1f, true);

            Assert.That(player.SpeedMultiplier, Is.EqualTo(1f + SlingshotState.BoostFraction).Within(1e-4f));
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Leader_IsLargestPlanetAboveMinimumAndGetsBountyOnKill()
        {
            yield return StartSharedMoonGame();
            Player hunter = PrepareMoonPlayer();
            hunter.Capacity = 1f;
            Enemy prey = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            prey.Capacity = 0.6f;
            prey.transform.position = hunter.transform.position;
            Physics2D.SyncTransforms();
            SetLeader(prey, false);
            float baseline = hunter.Capacity;
            TouchWith(hunter, prey.GetComponent<Collider2D>());
            float normalGain = hunter.Capacity - baseline;

            hunter.Capacity = 1f;
            prey.Capacity = 0.6f;
            SetLeader(prey, true);
            baseline = hunter.Capacity;
            TouchWith(hunter, prey.GetComponent<Collider2D>());
            float bountyGain = hunter.Capacity - baseline;

            Assert.That(normalGain, Is.GreaterThan(0f));
            Assert.That(bountyGain, Is.EqualTo(normalGain * CombatRules.LeaderBountyMultiplier).Within(1e-3f));
            yield return null;
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Leader_DirectoryPicksBiggestEligiblePlanet()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            Enemy[] enemies = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
            foreach (Enemy enemy in enemies)
            {
                enemy.Capacity = 0.2f;
            }

            player.Capacity = 1.2f;
            yield return new WaitForSeconds(PlanetDirectoryRefreshSeconds);
            yield return WaitUntil(() => player.IsLeader, 3f);

            Assert.That(player.IsLeader, Is.True);
            Assert.That(enemies.Any(enemy => enemy.IsLeader), Is.False);

            player.Capacity = 0.2f;
            yield return new WaitForSeconds(PlanetDirectoryRefreshSeconds);
            yield return WaitUntil(() => !player.IsLeader, 3f);

            Assert.That(player.IsLeader, Is.False, "Below the minimum capacity nobody leads.");
        }

        private const float PlanetDirectoryRefreshSeconds = 0.6f;

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static void GrantPerk(Player player, PerkId perk)
        {
            FieldInfo field = typeof(Player).GetField("_networkPerks", BindingFlags.Instance | BindingFlags.NonPublic);
            var variable = (NetworkVariable<ushort>)field.GetValue(player);
            variable.Value = PerkRules.With(variable.Value, perk);
            typeof(PlanetScale).GetMethod("RefreshMoonSlots", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, null);
        }

        private static void SetLeader(PlanetScale planet, bool leader)
        {
            FieldInfo field = typeof(PlanetScale).GetField("_networkLeader", BindingFlags.Instance | BindingFlags.NonPublic);
            ((NetworkVariable<bool>)field.GetValue(planet)).Value = leader;
        }
    }
}
