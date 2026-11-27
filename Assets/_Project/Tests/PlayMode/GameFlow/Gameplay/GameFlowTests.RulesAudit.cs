using System.Collections;
using System.Linq;
using NUnit.Framework;
using RD.Settings;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		[UnityTest, SharedWorld, Timeout(45000)]
		public IEnumerator Combat_EatsAnOverlappingPlanetOnceTheMassRatioReachesTheThreshold()
		{
			yield return StartSharedMoonGame();
			Player player = PrepareMoonPlayer();
			player.GetComponent<PlayerMovement>().enabled = false;
			player.GetComponent<PlanetGravity>().enabled = false;
			player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
			Enemy bot = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
			bot.GetComponent<EnemyMovement>().enabled = false;
			bot.GetComponent<PlanetGravity>().enabled = false;
			bot.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
			player.Capacity = 0.3f;
			bot.Capacity = 0.3f;
			bot.transform.position = player.transform.position;
			Physics2D.SyncTransforms();
			yield return new WaitForSeconds(0.3f);

			Assert.That(player.Capacity, Is.EqualTo(0.3f).Within(1e-4f), "Equal planets must not eat each other.");
			Assert.That(bot.Capacity, Is.EqualTo(0.3f).Within(1e-4f));

			player.Capacity = 0.34f;
			yield return new WaitForSeconds(0.3f);

			Assert.That(player.Capacity, Is.GreaterThan(0.4f), "The overlap sweep must eat a victim the player outgrew while touching it.");
			Assert.That(bot.Capacity, Is.EqualTo(bot.InitialCapacity).Within(1e-4f));
		}

		[UnityTest, SharedWorld, Timeout(45000)]
		public IEnumerator Bots_MediumAndHardPullFoodThroughTheMagnetAndEasyDoesNot()
		{
			yield return StartSharedMoonGame();
			ISettingsService settings = Resolve<ISettingsService>();
			int previous = settings.Get(GameSettingKeys.BotDifficulty);

			try
			{
				Player player = PrepareMoonPlayer();
				player.transform.position = new Vector2(80f, 80f);
				Enemy bot = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
				bot.GetComponent<EnemyMovement>().enabled = false;
				bot.GetComponent<PlanetGravity>().enabled = false;
				bot.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
				bot.Capacity = 1f;
				bot.transform.position = new Vector2(-40f, -40f);
				Physics2D.SyncTransforms();
				yield return new WaitForSeconds(0.8f);

				foreach (int difficulty in new[] { 0, 1, 2 })
				{
					settings.Set(GameSettingKeys.BotDifficulty, difficulty);
					float reach = FoodMagnetRules.Reach(bot.BodyRadius, FoodMagnetRules.BaseReach, 1f);
					float distance = bot.BodyRadius + (reach - bot.BodyRadius) * 0.6f;
					FoodRecord food = AllFood().First(candidate => candidate.Active && !candidate.IsDropped);
					food.ResetClaim();
					food.Capacity = 0.05f;
					food.SetNutrition(0.02f);
					food.Position = (Vector2)bot.transform.position + Vector2.right * distance;
					float before = bot.Capacity;

					yield return new WaitForSeconds(0.6f);

					bool fed = bot.Capacity > before + 0.01f;
					Assert.That(fed, Is.EqualTo(difficulty != 0), "Difficulty " + difficulty);
				}
			}
			finally
			{
				settings.Set(GameSettingKeys.BotDifficulty, previous);
			}
		}
	}
}
