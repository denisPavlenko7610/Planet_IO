using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class ProgressionRulesTests
    {
        private static RunSummary Run(int id, float seconds = 60f, int score = 35, int tier = 2, int kills = 0, int perks = 0) =>
            new(id, seconds, score, tier, kills, perks);

        [Test]
        public void Xp_CombinesTimeMassKillsAndTier()
        {
            Assert.That(XpRules.RunXp(Run(1, 60f, 40, 2, 1)), Is.EqualTo(30 + 20 + 25 + 20));
        }

        [Test]
        public void Xp_IgnoresNegativeInputsAndCapsTime()
        {
            Assert.That(XpRules.RunXp(Run(1, -5f, -10, 0, -3)), Is.Zero);
            Assert.That(XpRules.RunXp(Run(1, 100000f, 0, 1, 0)), Is.EqualTo((int)(XpRules.MaxCountedSeconds * 0.5f)));
        }

        [Test]
        public void Rank_ThresholdsGrowAndStartAtZero()
        {
            Assert.That(XpRules.RankStartXp(1), Is.Zero);
            Assert.That(XpRules.RankStartXp(2), Is.EqualTo(100));
            Assert.That(XpRules.RankStartXp(3), Is.EqualTo(240));
            Assert.That(XpRules.RankStartXp(4), Is.EqualTo(420));
            for (int rank = 2; rank <= XpRules.MaxRank; rank++)
            {
                Assert.That(XpRules.RankStartXp(rank) - XpRules.RankStartXp(rank - 1), Is.EqualTo(XpRules.StepXp(rank - 1)));
            }
        }

        [TestCase(0, 1)]
        [TestCase(99, 1)]
        [TestCase(100, 2)]
        [TestCase(239, 2)]
        [TestCase(240, 3)]
        [TestCase(int.MaxValue, 30)]
        public void Rank_ForXp(int xp, int expected)
        {
            Assert.That(XpRules.RankForXp(xp), Is.EqualTo(expected));
        }

        [Test]
        public void Rank_ProgressIsFractionOfCurrentStep()
        {
            Assert.That(XpRules.Progress(0), Is.Zero);
            Assert.That(XpRules.Progress(50), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(XpRules.Progress(100), Is.Zero);
            Assert.That(XpRules.Progress(int.MaxValue), Is.EqualTo(1f));
        }

        [Test]
        public void Dust_IsQuarterOfXpWithMinimumOne()
        {
            Assert.That(DustRules.RunDust(0), Is.Zero);
            Assert.That(DustRules.RunDust(1), Is.EqualTo(1));
            Assert.That(DustRules.RunDust(100), Is.EqualTo(25));
        }

        [Test]
        public void Dust_StreakRewardGrowsAndCaps()
        {
            Assert.That(DustRules.StreakReward(0), Is.EqualTo(10));
            Assert.That(DustRules.StreakReward(1), Is.EqualTo(10));
            Assert.That(DustRules.StreakReward(3), Is.EqualTo(20));
            Assert.That(DustRules.StreakReward(100), Is.EqualTo(DustRules.StreakMaxReward));
        }

        [Test]
        public void Missions_DefinitionsAreConsistent()
        {
            for (int definition = 0; definition < MissionRules.DefinitionCount; definition++)
            {
                Assert.That(MissionRules.IsValid(definition), Is.True);
                Assert.That(MissionRules.Target(definition), Is.GreaterThan(0));
                Assert.That(MissionRules.Reward(definition), Is.GreaterThan(0));
            }

            Assert.That(MissionRules.IsValid(-1), Is.False);
            Assert.That(MissionRules.IsValid(MissionRules.DefinitionCount), Is.False);
        }

        [Test]
        public void Missions_DailyPickIsDistinctAndDeterministic()
        {
            var first = new int[MissionRules.DailyCount];
            var second = new int[MissionRules.DailyCount];

            MissionRules.PickDaily(20000, first);
            MissionRules.PickDaily(20000, second);

            Assert.That(second, Is.EqualTo(first));
            Assert.That(first, Is.Unique);
            Assert.That(first, Is.All.InRange(0, MissionRules.DefinitionCount - 1));
        }

        [Test]
        public void Missions_DifferentDaysEventuallyDiffer()
        {
            var baseline = new int[MissionRules.DailyCount];
            MissionRules.PickDaily(20000, baseline);
            bool differs = false;
            for (int day = 20001; day < 20030 && !differs; day++)
            {
                var other = new int[MissionRules.DailyCount];
                MissionRules.PickDaily(day, other);
                differs = !System.Linq.Enumerable.SequenceEqual(other, baseline);
            }

            Assert.That(differs, Is.True);
        }

        [Test]
        public void Missions_CumulativeAndBestMetricsAdvanceDifferently()
        {
            Assert.That(MissionRules.Advance(MissionMetric.Kills, 2, 3), Is.EqualTo(5));
            Assert.That(MissionRules.Advance(MissionMetric.PeakTier, 3, 2), Is.EqualTo(3));
            Assert.That(MissionRules.Advance(MissionMetric.PeakTier, 2, 4), Is.EqualTo(4));
            Assert.That(MissionRules.Advance(MissionMetric.Runs, 1, -5), Is.EqualTo(1));
        }

        [Test]
        public void Missions_ReplacementAvoidsMissionsInUse()
        {
            int[] inUse = { 0, 1, 2 };

            for (int day = 0; day < 50; day++)
            {
                int replacement = MissionRules.PickReplacement(day, 1, inUse);

                Assert.That(inUse, Has.No.Member(replacement));
            }
        }

        [Test]
        public void State_FirstDayGrantsStreakAndMissions()
        {
            var state = new ProgressionState();

            int reward = state.RefreshDay(20000);

            Assert.That(reward, Is.EqualTo(DustRules.StreakReward(1)));
            Assert.That(state.Dust, Is.EqualTo(reward));
            Assert.That(state.StreakDays, Is.EqualTo(1));
            Assert.That(state.Missions.Length, Is.EqualTo(MissionRules.DailyCount));
        }

        [Test]
        public void State_SameDayDoesNotPayTwice()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            int dust = state.Dust;

            Assert.That(state.RefreshDay(20000), Is.Zero);
            Assert.That(state.Dust, Is.EqualTo(dust));
        }

        [Test]
        public void State_ConsecutiveDaysBuildStreakAndGapResetsIt()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.RefreshDay(20001);
            state.RefreshDay(20002);
            Assert.That(state.StreakDays, Is.EqualTo(3));

            state.RefreshDay(20005);

            Assert.That(state.StreakDays, Is.EqualTo(1));
        }

        [Test]
        public void State_ClockMovingBackwardsChangesNothing()
        {
            var state = new ProgressionState();
            state.RefreshDay(20010);
            int dust = state.Dust;

            Assert.That(state.RefreshDay(20005), Is.Zero);
            Assert.That(state.Dust, Is.EqualTo(dust));
            Assert.That(state.LastLoginDay, Is.EqualTo(20010));
        }

        [Test]
        public void State_NewDayRegeneratesMissionsAndRerollToken()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.RerollUsed = true;
            state.Missions[0].Progress = 99;

            state.RefreshDay(20001);

            Assert.That(state.RerollUsed, Is.False);
            Assert.That(System.Linq.Enumerable.Select(state.Missions, mission => mission.Progress), Is.All.Zero);
        }

        [Test]
        public void State_RunAddsXpDustAndRank()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            int dustBefore = state.Dust;

            RunReward reward = state.ApplyRun(Run(1, 200f, 100, 3, 2));

            int expectedXp = XpRules.RunXp(Run(1, 200f, 100, 3, 2));
            Assert.That(reward.XpGained, Is.EqualTo(expectedXp));
            Assert.That(state.Xp, Is.EqualTo(expectedXp));
            Assert.That(state.Dust, Is.EqualTo(dustBefore + DustRules.RunDust(expectedXp)));
            Assert.That(reward.RankedUp, Is.EqualTo(reward.RankAfter > reward.RankBefore));
            Assert.That(reward.RankAfter, Is.EqualTo(XpRules.RankForXp(expectedXp)));
        }

        [Test]
        public void State_SameRunNeverPaysTwiceButGrowthPaysTheDifference()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.ApplyRun(Run(7, 60f, 40, 2, 1));
            int xpAfterFirst = state.Xp;

            RunReward repeat = state.ApplyRun(Run(7, 60f, 40, 2, 1));
            RunReward longer = state.ApplyRun(Run(7, 160f, 90, 3, 2));

            Assert.That(repeat.XpGained, Is.Zero);
            Assert.That(state.Xp, Is.EqualTo(XpRules.RunXp(Run(7, 160f, 90, 3, 2))));
            Assert.That(longer.XpGained, Is.EqualTo(state.Xp - xpAfterFirst));
        }

        [Test]
        public void State_NewRunStartsFreshLedger()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.ApplyRun(Run(1, 100f, 50, 2, 0));
            int xp = state.Xp;

            RunReward second = state.ApplyRun(Run(2, 100f, 50, 2, 0));

            Assert.That(second.XpGained, Is.EqualTo(XpRules.RunXp(Run(2, 100f, 50, 2, 0))));
            Assert.That(state.Xp, Is.EqualTo(xp + second.XpGained));
        }

        [Test]
        public void State_MissionsProgressAndCompleteOnce()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.Missions[0].Definition = 0;
            state.Missions[1].Definition = 1;
            state.Missions[2].Definition = 5;

            RunReward first = state.ApplyRun(Run(1, 30f, 20, 3, 2));
            RunReward second = state.ApplyRun(Run(1, 40f, 25, 3, 3));

            Assert.That(state.Missions[0].Progress, Is.EqualTo(3), "Kills add only the delta of the same run.");
            Assert.That(state.Missions[1].IsComplete, Is.True, "Reaching tier 3 completes the tier mission.");
            Assert.That(state.Missions[2].Progress, Is.EqualTo(1), "A run counts once.");
            Assert.That(first.MissionsCompleted, Is.EqualTo(1));
            Assert.That(second.MissionsCompleted, Is.EqualTo(1));
        }

        [Test]
        public void State_ClaimPaysOnceAndOnlyWhenComplete()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.Missions[0].Definition = 1;
            int dust = state.Dust;

            Assert.That(state.TryClaim(0), Is.False);

            state.ApplyRun(Run(1, 10f, 10, 3));
            int afterRun = state.Dust;
            Assert.That(state.TryClaim(0), Is.True);
            Assert.That(state.Dust, Is.EqualTo(afterRun + MissionRules.Reward(1)));
            Assert.That(state.TryClaim(0), Is.False);
            Assert.That(state.Dust, Is.GreaterThan(dust));
        }

        [Test]
        public void State_ClaimRejectsBadIndex()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);

            Assert.That(state.TryClaim(-1), Is.False);
            Assert.That(state.TryClaim(MissionRules.DailyCount), Is.False);
        }

        [Test]
        public void State_RerollWorksOncePerDayOnUntouchedMission()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            int original = state.Missions[0].Definition;

            Assert.That(state.TryReroll(0), Is.True);
            Assert.That(state.Missions[0].Definition, Is.Not.EqualTo(original));
            Assert.That(state.TryReroll(1), Is.False, "Only one reroll per day.");
        }

        [Test]
        public void State_RerollRefusesStartedOrClaimedMissions()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.Missions[0].Progress = 1;
            state.Missions[1].Claimed = true;

            Assert.That(state.TryReroll(0), Is.False);
            Assert.That(state.TryReroll(1), Is.False);
            Assert.That(state.TryReroll(9), Is.False);
        }

        [Test]
        public void State_SpendRequiresEnoughDust()
        {
            var state = new ProgressionState { Dust = 100 };

            Assert.That(state.TrySpend(101), Is.False);
            Assert.That(state.Dust, Is.EqualTo(100));
            Assert.That(state.TrySpend(40), Is.True);
            Assert.That(state.Dust, Is.EqualTo(60));
            Assert.That(state.TrySpend(-1), Is.False);
        }

        [Test]
        public void State_AddDustIgnoresNegativeAmounts()
        {
            var state = new ProgressionState { Dust = 10 };

            state.AddDust(-50);
            state.AddDust(5);

            Assert.That(state.Dust, Is.EqualTo(15));
        }

        [Test]
        public void State_ValidationRejectsCorruptData()
        {
            var good = new ProgressionState();
            good.RefreshDay(20000);
            Assert.That(good.IsValid(), Is.True);

            Assert.That(new ProgressionState { Version = 99 }.IsValid(), Is.False);
            Assert.That(new ProgressionState { Xp = -1 }.IsValid(), Is.False);
            Assert.That(new ProgressionState { Dust = -1 }.IsValid(), Is.False);
            Assert.That(new ProgressionState { Missions = null }.IsValid(), Is.False);
            Assert.That(
                new ProgressionState { Missions = new[] { new MissionEntry { Definition = 99 } } }.IsValid(),
                Is.False);
        }

        [Test]
        public void State_SurvivesJsonRoundTrip()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            state.ApplyRun(Run(3, 120f, 60, 2, 1));

            ProgressionState copy = JsonUtility.FromJson<ProgressionState>(JsonUtility.ToJson(state));

            Assert.That(copy.IsValid(), Is.True);
            Assert.That(copy.Xp, Is.EqualTo(state.Xp));
            Assert.That(copy.Dust, Is.EqualTo(state.Dust));
            Assert.That(copy.Missions.Length, Is.EqualTo(MissionRules.DailyCount));
            Assert.That(copy.LedgerRunId, Is.EqualTo(3));
        }

        [Test]
        public void State_ClaimableFlagFollowsMissions()
        {
            var state = new ProgressionState();
            state.RefreshDay(20000);
            Assert.That(state.HasClaimableMission(), Is.False);

            state.Missions[0].Progress = int.MaxValue;
            Assert.That(state.HasClaimableMission(), Is.True);

            state.TryClaim(0);
            Assert.That(state.HasClaimableMission(), Is.False);
        }
    }

}
