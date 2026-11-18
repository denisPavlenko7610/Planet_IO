using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlanetIO.UI.Camera;
using PlanetIO.UI.Hud;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		[UnityTest, Timeout(45000)]
		public IEnumerator Bots_MoonRaidRetreatsOnTimeoutAndDamageAndHonorsCooldown()
		{
			yield return StartMoonGame();

			Player target = PrepareMoonPlayer();
			target.Capacity = 0.8f;
			Enemy bot = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
			bot.Capacity = 0.4f;
			bot.transform.position = target.transform.position + Vector3.right * 12f;
			EnemyMovement movement = bot.GetComponent<EnemyMovement>();
			movement.enabled = false;
			var settings = Resolve<RD.Settings.ISettingsService>();
			int previous = settings.Get(GameSettingKeys.BotDifficulty);
			try
			{
				settings.Set(GameSettingKeys.BotDifficulty, (int)BotDifficulty.Hard);
				typeof(EnemyMovement).GetMethod("ApplyDifficulty", BindingFlags.Instance | BindingFlags.NonPublic)
					.Invoke(movement, null);
				MethodInfo attack = typeof(EnemyMovement).GetMethod(
					"TryMoonRaid",
					BindingFlags.Instance | BindingFlags.NonPublic
				);
				MethodInfo step = typeof(EnemyMovement).GetMethod(
					"UpdateMoonRaid",
					BindingFlags.Instance | BindingFlags.NonPublic
				);
				SetMoonField(movement, "_raidCooldown", 0f);
				Assert.That(attack.Invoke(movement, new object[] { target, false }), Is.True);
				Assert.That(movement.State, Is.EqualTo(EnemyMovement.MovementState.Hunting));
				step.Invoke(movement, new object[] { MoonRaidRules.CommitmentSeconds + 0.1f });
				Assert.That(movement.State, Is.EqualTo(EnemyMovement.MovementState.Evading));
				Assert.That(attack.Invoke(movement, new object[] { target, false }), Is.False);
				SetMoonField(movement, "_raidCooldown", 0f);
				Assert.That(attack.Invoke(movement, new object[] { target, false }), Is.True);
				bot.Capacity -= 0.01f;
				step.Invoke(movement, new object[] { 0.02f });
				Assert.That(movement.State, Is.EqualTo(EnemyMovement.MovementState.Evading));
			}
			finally
			{
				settings.Set(GameSettingKeys.BotDifficulty, previous);
			}
		}

		[UnityTest, Timeout(45000)]
		public IEnumerator Damage_ConfirmedHitFlashesShakesAndShowsLossButBoostAndResetsDoNot()
		{
			yield return StartMoonGame();

			Player player = PrepareMoonPlayer();
			player.Capacity = 1f;
			player.GetComponent<PlayerMovement>().enabled = false;
			player.GetComponent<PlanetGravity>().enabled = false;
			player.GetComponent<Collider2D>().enabled = false;
			player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
			SessionHudView hud = Object.FindFirstObjectByType<SessionHudView>();
			VisualElement root = hud.GetComponent<UIDocument>().rootVisualElement;
			Label popup = root.Q<Label>("damage-popup");
			PlayerCamera camera = Camera.main.GetComponent<PlayerCamera>();
			int hits = 0;
			float lost = 0f;
			player.Damaged += (amount, _) =>
			{
				hits++;
				lost = amount;
			};

			player.Capacity = 0.9f;
			player.SetBoostRpc(true);
			typeof(Player).GetMethod("UpdateBoost", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(player, new object[] { 0.15f });
			player.SetBoostRpc(false);
			Assert.That(player.Capacity, Is.LessThan(0.9f));
			Assert.That(hits, Is.Zero);
			Assert.That(popup.resolvedStyle.opacity, Is.Zero);

			player.Capacity = 1f;
			SetMoonField(player, "_pendingOffers", 0);
			SetMoonField(player, "_offerCount", 0);
			hud.HidePerkOffer();
			yield return new WaitForSecondsRealtime(0.8f);

			Assert.That(player.TryApplyHazard(0.1f, out float dealt), Is.True);
			Assert.That(hits, Is.EqualTo(1));
			Assert.That(lost, Is.EqualTo(dealt).Within(0.00001f));
			Assert.That(popup.text, Is.EqualTo($"−{Mathf.Max(1, Constants.CapacityToScore(dealt)):N0}"));
			Assert.That(
				(float)typeof(PlayerCamera).GetField(
						"_shakeTimeRemaining",
						BindingFlags.Instance | BindingFlags.NonPublic
					)
					.GetValue(camera),
				Is.GreaterThan(0f)
			);
			yield return null;

			Assert.That(popup.resolvedStyle.opacity, Is.GreaterThan(0.5f));
			Assert.That(root.Q("border-warning").resolvedStyle.opacity, Is.GreaterThan(0f));
			ScreenCapture.CaptureScreenshot("Temp/DamageFeedback.png");
			yield return null;
			yield return new WaitForSecondsRealtime(1f);

			Assert.That(popup.resolvedStyle.opacity, Is.Zero);
			Assert.That(root.Q("border-warning").resolvedStyle.opacity, Is.Zero);

			hud.SetBorderWarning(0.4f);
			hud.ShowDamage(new Vector2(Screen.width / 2f, Screen.height / 2f), 10, 0.1f);
			hud.ClearDamage();
			Assert.That(root.Q("border-warning").style.opacity.value, Is.EqualTo(0.4f));
			hud.ShowDefeat("Defeat", "");
			Assert.That(popup.style.opacity.value, Is.Zero);
		}

		[UnityTest, Timeout(45000)]
		public IEnumerator Moons_RepeatedRealClashesBreakOnlyContactingMoonsAndOpenCoreAttack()
		{
			yield return StartMoonGame();

			Player small = PrepareMoonPlayer();
			Enemy large = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
			large.Capacity = 0.8f;
			small.GetComponent<PlayerMovement>().enabled = false;
			large.GetComponent<EnemyMovement>().enabled = false;
			float a = small.BodyRadius;
			float b = large.BodyRadius;
			Vector2 smallMoon = MoonRules.Position(
				Vector2.zero,
				a,
				0,
				small.MoonSlots,
				OrbitClock.Now,
				small.MoonPhaseDegrees
			);
			Vector2 largeMoon = MoonRules.Position(
				Vector2.zero,
				b,
				0,
				large.MoonSlots,
				OrbitClock.Now,
				large.MoonPhaseDegrees
			);
			large.transform.position = small.transform.position + (Vector3)(smallMoon - largeMoon);
			Physics2D.SyncTransforms();
			MethodInfo clash = typeof(PlanetScale).GetMethod(
				"ResolveMoonClashes",
				BindingFlags.Instance | BindingFlags.NonPublic
			);
			for (int hit = 0; hit < MoonRules.MaxDurability; hit++)
			{
				SetMoonField(small, "_clashCooldown", 0f);
				SetMoonField(large, "_clashCooldown", 0f);
				clash.Invoke(small, null);
			}
			Assert.That(small.MoonMask, Is.EqualTo(2));
			Assert.That(large.MoonMask, Is.EqualTo(14));
			Assert.That(small.Capacity, Is.EqualTo(0.4f));
			Assert.That(large.Capacity, Is.EqualTo(0.8f));

			// A live moon can touch the larger core while the smaller core remains outside it.
			Vector2 attackOffset = MoonRules.Position(
				Vector2.zero,
				a,
				1,
				small.MoonSlots,
				OrbitClock.Now,
				small.MoonPhaseDegrees
			);
			large.transform.position = small.transform.position +
				(Vector3)(attackOffset.normalized * MoonRaidRules.StandOff(a, b));
			Physics2D.SyncTransforms();
			Assert.That(Vector2.Distance(small.transform.position, large.transform.position), Is.GreaterThan(a + b));
			SetMoonField(small, "_clashStepTimer", 10f);
			StepMoons(small);
			Assert.That(large.Capacity, Is.LessThan(0.8f));
			Assert.That(small.Capacity, Is.GreaterThan(0.4f));
		}
	}
}