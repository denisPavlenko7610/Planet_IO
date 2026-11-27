using NUnit.Framework;

namespace PlanetIO.Tests
{
	public sealed class PlatformServiceBoundaryTests
	{
		private static RunSummary Run(float seconds = 120f, int score = 300, int tier = 2, int kills = 3) =>
			new(1, seconds, score, tier, kills, 0);

		[Test]
		public void Review_PromptsAtExactlyTheRunAndDayThresholds()
		{
			var state = new ServiceState { RunsFinished = ReviewPromptRules.MinRuns, FirstRunDay = 100 };

			Assert.That(ReviewPromptRules.ShouldPrompt(state, true, 100 + ReviewPromptRules.MinDaysSinceFirstRun), Is.True);
			Assert.That(ReviewPromptRules.ShouldPrompt(state, true, 100 + ReviewPromptRules.MinDaysSinceFirstRun - 1), Is.False);
		}

		[Test]
		public void Review_NullStateNeverPrompts()
		{
			Assert.That(ReviewPromptRules.ShouldPrompt(null, true, 500), Is.False);
		}

		[Test]
		public void Score_DurationBoundaryIsInclusive()
		{
			Assert.That(ScoreValidationRules.IsPlausible(Run(seconds: ScoreValidationRules.MinRunSeconds, kills: 0)), Is.True);
			Assert.That(ScoreValidationRules.IsPlausible(Run(seconds: ScoreValidationRules.MinRunSeconds - 0.01f, kills: 0)), Is.False);
		}

		[Test]
		public void Score_ScoreBoundariesAreInclusive()
		{
			Assert.That(ScoreValidationRules.IsPlausible(Run(score: 0)), Is.True);
			Assert.That(ScoreValidationRules.IsPlausible(Run(score: ScoreValidationRules.MaxScore)), Is.True);
			Assert.That(ScoreValidationRules.IsPlausible(Run(score: -1)), Is.False);
		}

		[Test]
		public void Score_MaxKillsWithNonPositiveDurationIsJustTheSlack()
		{
			Assert.That(ScoreValidationRules.MaxKills(0f), Is.EqualTo(ScoreValidationRules.KillSlack));
			Assert.That(ScoreValidationRules.MaxKills(-30f), Is.EqualTo(ScoreValidationRules.KillSlack));
		}

		[TestCase(AchievementKind.Kills5, 5, 4)]
		[TestCase(AchievementKind.Kills15, 15, 14)]
		public void Achievements_KillThresholdsAreInclusive(AchievementKind kind, int atThreshold, int below)
		{
			Assert.That(AchievementRules.IsEarned(kind, Run(kills: atThreshold), 1, 0), Is.True);
			Assert.That(AchievementRules.IsEarned(kind, Run(kills: below), 1, 0), Is.False);
		}

		[TestCase(AchievementKind.TierPlanet, 2)]
		[TestCase(AchievementKind.TierGasGiant, 3)]
		[TestCase(AchievementKind.TierStar, 4)]
		public void Achievements_TierThresholdsAreInclusive(AchievementKind kind, int threshold)
		{
			Assert.That(AchievementRules.IsEarned(kind, Run(tier: threshold), 1, 0), Is.True);
			Assert.That(AchievementRules.IsEarned(kind, Run(tier: threshold - 1), 1, 0), Is.False);
		}

		[Test]
		public void Achievements_SurviveThresholdIsInclusive()
		{
			Assert.That(AchievementRules.IsEarned(AchievementKind.Survive5Min, Run(seconds: AchievementRules.SurviveSeconds), 1, 0), Is.True);
			Assert.That(AchievementRules.IsEarned(AchievementKind.Survive5Min, Run(seconds: AchievementRules.SurviveSeconds - 0.01f), 1, 0), Is.False);
		}

		[TestCase(AchievementKind.Rank5, 5, 4)]
		[TestCase(AchievementKind.Rank15, 15, 14)]
		public void Achievements_RankThresholdsAreInclusive(AchievementKind kind, int atThreshold, int below)
		{
			Assert.That(AchievementRules.IsEarned(kind, Run(), atThreshold, 0), Is.True);
			Assert.That(AchievementRules.IsEarned(kind, Run(), below, 0), Is.False);
		}

		[Test]
		public void Achievements_StreakThresholdIsInclusive()
		{
			Assert.That(AchievementRules.IsEarned(AchievementKind.Streak7, Run(), 1, AchievementRules.StreakDays), Is.True);
			Assert.That(AchievementRules.IsEarned(AchievementKind.Streak7, Run(), 1, AchievementRules.StreakDays - 1), Is.False);
		}

		[Test]
		public void CloudWins_IdenticalProgressionKeepsLocal()
		{
			var local = new ProgressionState { Xp = 40, LastLoginDay = 7 };
			var cloud = new ProgressionState { Xp = 40, LastLoginDay = 7 };

			Assert.That(CloudSaveRules.CloudWins(local, cloud), Is.False);
		}
	}
}
