using System;
using System.Linq;
using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class PlatformServiceRulesTests
    {
        private static ServiceState Veteran(int runs = 6, int firstDay = 100) =>
            new() { RunsFinished = runs, FirstRunDay = firstDay };

        private static RunSummary Run(long id = 1, float seconds = 120f, int score = 300, int tier = 2, int kills = 3) =>
            new(id, seconds, score, tier, kills, 0);

        [Test]
        public void Review_RequiresARankUp()
        {
            Assert.That(ReviewPromptRules.ShouldPrompt(Veteran(), false, 110), Is.False);
            Assert.That(ReviewPromptRules.ShouldPrompt(Veteran(), true, 110), Is.True);
        }

        [Test]
        public void Review_WaitsForRunsAndDays()
        {
            Assert.That(ReviewPromptRules.ShouldPrompt(Veteran(ReviewPromptRules.MinRuns - 1), true, 110), Is.False);
            Assert.That(ReviewPromptRules.ShouldPrompt(Veteran(firstDay: 109), true, 110), Is.False);
            Assert.That(ReviewPromptRules.ShouldPrompt(Veteran(firstDay: 0), true, 110), Is.False);
        }

        [Test]
        public void Review_RespectsCooldownAndLifetimeCap()
        {
            ServiceState state = Veteran();
            state.MarkReviewPrompted(110);

            Assert.That(ReviewPromptRules.ShouldPrompt(state, true, 110 + ReviewPromptRules.MinDaysBetweenPrompts - 1), Is.False);
            Assert.That(ReviewPromptRules.ShouldPrompt(state, true, 110 + ReviewPromptRules.MinDaysBetweenPrompts), Is.True);

            state.ReviewPrompts = ReviewPromptRules.MaxPrompts;
            Assert.That(ReviewPromptRules.ShouldPrompt(state, true, 5000), Is.False);
        }

        [Test]
        public void Score_RejectsImplausibleRuns()
        {
            Assert.That(ScoreValidationRules.IsPlausible(Run()), Is.True);
            Assert.That(ScoreValidationRules.IsPlausible(Run(seconds: 5f)), Is.False);
            Assert.That(ScoreValidationRules.IsPlausible(Run(score: ScoreValidationRules.MaxScore + 1)), Is.False);
            Assert.That(ScoreValidationRules.IsPlausible(Run(seconds: 20f, kills: 200)), Is.False);
            Assert.That(ScoreValidationRules.IsPlausible(Run(kills: -1)), Is.False);
        }

        [Test]
        public void Score_AcceptsTheKillRateLimitExactly()
        {
            int limit = ScoreValidationRules.MaxKills(60f);

            Assert.That(ScoreValidationRules.IsPlausible(Run(seconds: 60f, kills: limit)), Is.True);
            Assert.That(ScoreValidationRules.IsPlausible(Run(seconds: 60f, kills: limit + 1)), Is.False);
        }

        [Test]
        public void Score_EasyDifficultyDoesNotCountForRanking()
        {
            Assert.That(ScoreValidationRules.CountsForRanking(BotDifficulty.Easy), Is.False);
            Assert.That(ScoreValidationRules.CountsForRanking(BotDifficulty.Medium), Is.True);
            Assert.That(ScoreValidationRules.CountsForRanking(BotDifficulty.Hard), Is.True);
        }

        [Test]
        public void Achievements_MatchTheirThresholds()
        {
            RunSummary strong = Run(seconds: 301f, score: 500, tier: 4, kills: 15);

            foreach (AchievementKind kind in new[]
                     {
                         AchievementKind.TierPlanet, AchievementKind.TierGasGiant, AchievementKind.TierStar,
                         AchievementKind.Kills5, AchievementKind.Kills15, AchievementKind.Survive5Min
                     })
            {
                Assert.That(AchievementRules.IsEarned(kind, strong, 1, 0), Is.True, kind.ToString());
            }

            RunSummary weak = Run(seconds: 30f, score: 20, tier: 1, kills: 0);
            foreach (AchievementKind kind in Enum.GetValues(typeof(AchievementKind)).Cast<AchievementKind>())
            {
                Assert.That(AchievementRules.IsEarned(kind, weak, 1, 0), Is.False, kind.ToString());
            }
        }

        [Test]
        public void Achievements_RankAndStreakUseProgressionNotTheRun()
        {
            RunSummary weak = Run(seconds: 30f, score: 20, tier: 1, kills: 0);

            Assert.That(AchievementRules.IsEarned(AchievementKind.Rank5, weak, 5, 0), Is.True);
            Assert.That(AchievementRules.IsEarned(AchievementKind.Rank15, weak, 14, 0), Is.False);
            Assert.That(AchievementRules.IsEarned(AchievementKind.Rank30, weak, XpRules.MaxRank, 0), Is.True);
            Assert.That(AchievementRules.IsEarned(AchievementKind.Streak7, weak, 1, AchievementRules.StreakDays), Is.True);
        }

        [Test]
        public void ServiceState_CountsEachRunOnceAndEveryDeath()
        {
            var state = new ServiceState();

            Assert.That(state.RegisterDeath(Run(7, kills: 2), 50), Is.True);
            Assert.That(state.RegisterDeath(Run(7, kills: 5), 50), Is.False);
            Assert.That(state.RegisterDeath(Run(8, kills: 1), 51), Is.True);

            Assert.That(state.RunsFinished, Is.EqualTo(2));
            Assert.That(state.TotalKills, Is.EqualTo(6));
            Assert.That(state.FirstRunDay, Is.EqualTo(50));
        }

        [Test]
        public void ServiceState_ReplayingTheSameSummaryDoesNotInflateKills()
        {
            var state = new ServiceState();

            state.RegisterDeath(Run(3, kills: 4), 10);
            state.RegisterDeath(Run(3, kills: 4), 10);

            Assert.That(state.TotalKills, Is.EqualTo(4));
        }

        [Test]
        public void ServiceState_AchievementsAreDeduplicatedAndSorted()
        {
            var state = new ServiceState();

            Assert.That(state.AddAchievement(AchievementKind.Rank5), Is.True);
            Assert.That(state.AddAchievement(AchievementKind.Rank5), Is.False);
            state.AddAchievement(AchievementKind.Kills5);
            state.MergeAchievements(new[] { "Rank5", "TierStar", "" });

            Assert.That(state.Achievements, Is.EqualTo(new[] { "Kills5", "Rank5", "TierStar" }));
            Assert.That(state.HasAchievement(AchievementKind.TierStar), Is.True);
        }

        [Test]
        public void ServiceState_RejectsNegativeCounters()
        {
            Assert.That(new ServiceState().IsValid(), Is.True);
            Assert.That(new ServiceState { RunsFinished = -1 }.IsValid(), Is.False);
            Assert.That(new ServiceState { Version = 99 }.IsValid(), Is.False);
            Assert.That(new ServiceState { Achievements = null }.IsValid(), Is.False);
        }
    }
}
