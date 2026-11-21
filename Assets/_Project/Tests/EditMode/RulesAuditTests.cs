using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
	public sealed class RulesAuditTests
	{
		[Test]
		public void InContact_IsTrueWhenBodiesTouchOrOverlapAndFalseWhenApart()
		{
			Assert.That(CombatRules.InContact(Vector2.zero, 1f, new Vector2(1.5f, 0f), 1f), Is.True);
			Assert.That(CombatRules.InContact(Vector2.zero, 1f, new Vector2(2f, 0f), 1f), Is.True);
			Assert.That(CombatRules.InContact(Vector2.zero, 1f, new Vector2(2.01f, 0f), 1f), Is.False);
		}

		[Test]
		public void InContact_IgnoresNegativeRadii()
		{
			Assert.That(CombatRules.InContact(Vector2.zero, -5f, new Vector2(1f, 0f), 0.5f), Is.False);
			Assert.That(CombatRules.InContact(Vector2.zero, -5f, new Vector2(0.4f, 0f), 0.5f), Is.True);
		}

		[Test]
		public void LootSourceCapacity_RestoresTheLostMassThroughTheLootFraction()
		{
			float source = CombatRules.LootSourceCapacity(0.7f);

			Assert.That(source * CombatRules.LootMassFraction, Is.EqualTo(0.7f).Within(1e-5f));
			Assert.That(CombatRules.LootSourceCapacity(-1f), Is.EqualTo(0f));
		}

		[Test]
		public void BoostCost_StaysFlatForSmallPlanetsAndScalesWithMassBeyondTwo()
		{
			Assert.That(BoostRules.Cost(0.1f, 0.004f, 1f), Is.EqualTo(0.004f));
			Assert.That(BoostRules.Cost(1f, 0.004f, 1f), Is.EqualTo(0.004f));
			Assert.That(BoostRules.Cost(2f, 0.004f, 1f), Is.EqualTo(0.004f).Within(1e-6f));
			Assert.That(BoostRules.Cost(4f, 0.004f, 1f), Is.EqualTo(0.008f).Within(1e-6f));
			Assert.That(BoostRules.Cost(8f, 0.004f, 1f), Is.EqualTo(0.016f).Within(1e-6f));
		}

		[Test]
		public void BoostCost_AppliesThePerkMultiplierAndRejectsBadInput()
		{
			Assert.That(BoostRules.Cost(8f, 0.004f, 1.25f), Is.EqualTo(0.02f).Within(1e-6f));
			Assert.That(BoostRules.Cost(-3f, 0.004f, 1f), Is.EqualTo(0.004f));
			Assert.That(BoostRules.Cost(1f, 0.004f, -1f), Is.EqualTo(0f));
		}

		[Test]
		public void BoostCost_NeverMakesBigPlanetsPayLessThanSmallOnes()
		{
			float previous = 0f;

			for (float capacity = 0.08f; capacity <= 8f; capacity += 0.05f)
			{
				float cost = BoostRules.Cost(capacity, 0.004f, 1f);

				Assert.That(cost, Is.GreaterThanOrEqualTo(previous));
				previous = cost;
			}
		}

		[Test]
		public void Bots_BotBoostUsesTheSameCostRule()
		{
			Assert.That(BotDifficultyRules.BoostCostFor(1f), Is.EqualTo(BotDifficultyRules.BoostCost));
			Assert.That(BotDifficultyRules.BoostCostFor(8f), Is.EqualTo(0.016f).Within(1e-6f));
			Assert.That(BotDifficultyRules.CanAffordBoost(8f, 0.08f), Is.True);
			Assert.That(BotDifficultyRules.CanAffordBoost(0.084f, 0.08f), Is.False);
		}

		[Test]
		public void Bots_MediumAndHardFeedLikeThePlayerAndEasyKeepsTheHandicap()
		{
			const float playerMultiplier = 0.015f;
			const float botBaseMultiplier = 0.01f;

			Assert.That(botBaseMultiplier * BotDifficultyRules.FoodGrowthScale(BotDifficulty.Medium), Is.EqualTo(playerMultiplier).Within(1e-6f));
			Assert.That(botBaseMultiplier * BotDifficultyRules.FoodGrowthScale(BotDifficulty.Hard), Is.EqualTo(playerMultiplier).Within(1e-6f));
			Assert.That(BotDifficultyRules.FoodGrowthScale(BotDifficulty.Easy), Is.EqualTo(1f));
		}

		[Test]
		public void Bots_OnlyMediumAndHardUseTheFoodMagnet()
		{
			Assert.That(BotDifficultyRules.UsesFoodMagnet(BotDifficulty.Easy), Is.False);
			Assert.That(BotDifficultyRules.UsesFoodMagnet(BotDifficulty.Medium), Is.True);
			Assert.That(BotDifficultyRules.UsesFoodMagnet(BotDifficulty.Hard), Is.True);
		}

		[Test]
		public void CometRider_HealsAFifthOfTheDamageOnceEveryFiveSecondsAndCostsFood()
		{
			ushort rider = PerkRules.Bit(PerkId.CometRider);

			Assert.That(PerkRules.CometHealFraction(rider), Is.EqualTo(0.2f));
			Assert.That(PerkRules.CometHealCooldownSeconds, Is.EqualTo(5f));
			Assert.That(PerkRules.FoodMultiplier(rider), Is.EqualTo(0.9f).Within(1e-6f));
			Assert.That(CometRules.Damage(1f, CometRules.DefaultDamageFraction) * PerkRules.CometHealFraction(rider), Is.EqualTo(0.1f).Within(1e-6f));
		}
	}
}
