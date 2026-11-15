using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		[UnityTest]
		[Timeout(45000)]
		public IEnumerator Boost_RequiresSurplusMassAndStopsAfterLastAffordableTick()
		{
			yield return StartSoloGame();
			yield return WaitForGameplay(10f);

			Player player = FindLocalPlayer();
			PlayerMovement movement = player.GetComponent<PlayerMovement>();
			float floor = Mathf.Max(player.InitialCapacity, player.MinCapacity);
			float cost = ReadPrivateField<float>(player, "_boostMassCost");
			player.GetComponent<Collider2D>().enabled = false;

			Assert.That(player.IsDefeated, Is.False);
			Assert.That(Constants.CapacityToScore(player.InitialCapacity), Is.EqualTo(8));

			player.Capacity = floor;
			movement.SendMessage("OnBoostChanged", true);
			yield return new WaitForFixedUpdate();

			Assert.That(player.CanBoost, Is.False);
			Assert.That(player.IsBoosting, Is.False);
			Assert.That(player.Capacity, Is.EqualTo(floor).Within(0.00001f));

			player.Capacity = floor + cost * 0.5f;
			movement.SendMessage("OnBoostChanged", true);
			yield return new WaitForFixedUpdate();

			Assert.That(player.CanBoost, Is.False);
			Assert.That(player.IsBoosting, Is.False);
			Assert.That(player.Capacity, Is.EqualTo(floor + cost * 0.5f).Within(0.00001f));

			player.Capacity = floor + cost * 1.5f;
			movement.SendMessage("OnBoostChanged", true);
			Assert.That(player.IsBoosting, Is.True);
			float beforePayment = player.Capacity;
			float deadline = Time.realtimeSinceStartup + 2f;
			while (player.Capacity >= beforePayment && Time.realtimeSinceStartup < deadline)
			{
				yield return null;
			}
			yield return new WaitForFixedUpdate();

			Assert.That(player.CanBoost, Is.False);
			Assert.That(player.IsBoosting, Is.False, "Stop immediately after the last affordable payment.");
			Assert.That(player.Capacity, Is.EqualTo(floor + cost * 0.5f).Within(0.00001f));
			Assert.That(
				player.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
				Is.LessThanOrEqualTo(ReadPrivateField<float>(movement, "_normalSpeed") + 0.01f)
			);

			yield return LeaveToMenu();
		}

		[UnityTest]
		[Timeout(45000)]
		public IEnumerator MinimumStartingMass_SurvivesRespawnAndStillDiesFromDamage()
		{
			yield return StartSoloGame();
			yield return WaitForGameplay(10f);

			Player player = FindLocalPlayer();
			Assert.That(player.InitialCapacity, Is.EqualTo(player.MinCapacity));
			Assert.That(player.IsDefeated, Is.False);
			player.Defeat();
			player.RespawnRpc();
			Assert.That(player.IsDefeated, Is.False);
			Assert.That(player.Capacity, Is.EqualTo(player.MinCapacity));
			Assert.That(player.CanBoost, Is.False);

			player.Capacity = 0f;
			Assert.That(
				player.IsDefeated,
				Is.True,
				"Damage below the minimum must still defeat a player already at that minimum."
			);

			yield return LeaveToMenu();
		}
	}
}