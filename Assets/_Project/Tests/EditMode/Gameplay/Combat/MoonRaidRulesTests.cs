using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
	public sealed class MoonRaidRulesTests
	{
		[TestCase(BotDifficulty.Easy, false, false)]
		[TestCase(BotDifficulty.Medium, true, false)]
		[TestCase(BotDifficulty.Medium, false, true)]
		[TestCase(BotDifficulty.Hard, true, true)]
		public void Raid_RespectsDifficultyAndCaution(BotDifficulty difficulty, bool cautious, bool expected)
		{
			Assert.That(MoonRaidRules.CanRaid(0.4f, 0.8f, true, difficulty, cautious, false, 0f), Is.EqualTo(expected));
		}

		[TestCase(false, false, 0f, 0.8f)]
		[TestCase(true, true, 0f, 0.8f)]
		[TestCase(true, false, 1f, 0.8f)]
		[TestCase(true, false, 0f, 1.3f)]
		[TestCase(true, false, 0f, 0.3f)]
		public void Raid_RejectsUnsafeOrUnnecessaryAttacks(bool armed, bool hazard, float cooldown, float target)
		{
			Assert.That(
				MoonRaidRules.CanRaid(0.4f, target, armed, BotDifficulty.Hard, false, hazard, cooldown),
				Is.False
			);
		}

		[TestCase(0.2f, 0.4f)]
		[TestCase(0.5f, 0.8f)]
		[TestCase(1f, 3f)]
		public void StandOff_AllowsMoonContactWithoutCoreOverlap(float ownRadius, float targetRadius)
		{
			float distance = MoonRaidRules.StandOff(ownRadius, targetRadius);
			Assert.That(distance, Is.GreaterThan(ownRadius + targetRadius));
			Assert.That(distance, Is.LessThan(MoonRules.OuterExtent(ownRadius) + targetRadius));
		}

		[Test]
		public void Raid_RetreatsOnHitTimeoutOrMissingMoons()
		{
			Assert.That(MoonRaidRules.ShouldRetreat(1f, 0.4f, 0.4f, true), Is.False);
			Assert.That(MoonRaidRules.ShouldRetreat(0f, 0.4f, 0.4f, true), Is.True);
			Assert.That(MoonRaidRules.ShouldRetreat(1f, 0.4f, 0.39f, true), Is.True);
			Assert.That(MoonRaidRules.ShouldRetreat(1f, 0.4f, 0.4f, false), Is.True);
		}

		[Test]
		public void SlowerMovement_PreservesGravityPullRatioAndBoostAdvantage()
		{
			Assert.That(GravityRules.MaxPullSpeed / MovementRules.PlayerSpeed, Is.EqualTo(1.4f / 4.8f).Within(0.0001f));
			Assert.That(MovementRules.PlayerBoostSpeed / MovementRules.PlayerSpeed, Is.EqualTo(2f));
		}

		[Test]
		public void Raid_SteersAroundCoreInsteadOfCrossingItToReachAnOrbitPoint()
		{
			Vector2 position = Vector2.left * 2.05f;
			Vector2 direction = MoonRaidRules.AvoidCore(position, Vector2.zero, 2f, Vector2.right, 0.1f);
			Assert.That((position + direction * 0.1f).magnitude, Is.GreaterThanOrEqualTo(2f));
			Assert.That(
				MoonRaidRules.AvoidCore(Vector2.left * 4f, Vector2.zero, 2f, Vector2.right, 0.1f),
				Is.EqualTo(Vector2.right)
			);
			Assert.That(
				MoonRaidRules.AvoidCore(Vector2.left, Vector2.zero, 2f, Vector2.right, 0.1f),
				Is.EqualTo(Vector2.left)
			);
		}
	}
}