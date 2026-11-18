using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using PlanetIO.Application;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class PostRunServiceTests
    {
        private sealed class StateStub : IServiceStateService
        {
            public event Action Changed;
            public bool IsLoaded => true;
            public ServiceState State { get; } = new();
            public int Dirty;

            public Awaitable InitializeAsync(CancellationToken cancellationToken = default) => default;

            public void MarkDirty()
            {
                Dirty++;
                Changed?.Invoke();
            }

            public Awaitable FlushAsync() => default;
        }

        private sealed class PlayGamesStub : IPlayGamesService
        {
            public event Action StateChanged;
            public bool IsAvailable => true;
            public bool IsSignedIn => true;
            public readonly Dictionary<LeaderboardKind, long> Scores = new();
            public readonly List<AchievementKind> Unlocked = new();

            public void SignIn(bool interactive, Action<bool> completed) => completed?.Invoke(true);
            public void ReportScore(LeaderboardKind board, long score) => Scores[board] = score;
            public void Unlock(AchievementKind achievement) => Unlocked.Add(achievement);
            public void ShowLeaderboards() { }
            public void ShowAchievements() { }
            public void LoadCloud(Action<CloudLoadStatus, byte[]> completed) => completed?.Invoke(CloudLoadStatus.Failed, null);
            public void SaveCloud(byte[] data, TimeSpan playedTime, Action<bool> completed) => completed?.Invoke(false);
            public void RaiseStateChanged() => StateChanged?.Invoke();
        }

        private sealed class ReviewStub : IReviewService
        {
            public bool IsAvailable { get; set; } = true;
            public int Requests;

            public void Request() => Requests++;
        }

        private sealed class ProgressionStub : IProgressionService
        {
            public event Action Changed;
            public int Xp => 0;
            public int Rank { get; set; } = 3;
            public float RankProgress => 0f;
            public int Dust => 0;
            public int StreakDays { get; set; }
            public int LastStreakReward => 0;
            public bool CanReroll => false;
            public bool HasClaimableMission => false;
            public IReadOnlyList<MissionView> Missions { get; } = new List<MissionView>();

            public void RefreshDay() => Changed?.Invoke();
            public RunReward ApplyRun(RunSummary summary) => default;
            public bool TryClaimMission(int index) => false;
            public bool TryRerollMission(int index) => false;
            public bool TrySpendDust(int amount) => false;
            public void AddDust(int amount) { }
        }

        private sealed class DifficultyStub : IRunDifficultyProvider
        {
            public BotDifficulty Current { get; set; } = BotDifficulty.Medium;
        }

        private sealed class CloudStub : ICloudSyncService
        {
            public int Uploads;

            public void SignIn(bool interactive, Action<bool> completed = null) => completed?.Invoke(true);
            public void SyncNow() { }
            public void RequestUpload() => Uploads++;
            public void FlushPending() { }
        }

        private sealed class AnalyticsStub : IAnalyticsService
        {
            public readonly List<string> Events = new();

            public void Track(string eventName, params (string Key, object Value)[] parameters) => Events.Add(eventName);
        }

        private StateStub _state;
        private PlayGamesStub _games;
        private ReviewStub _review;
        private ProgressionStub _progression;
        private DifficultyStub _difficulty;
        private CloudStub _cloud;
        private AnalyticsStub _analytics;
        private PostRunService _service;

        [SetUp]
        public void SetUp()
        {
            _state = new StateStub();
            _games = new PlayGamesStub();
            _review = new ReviewStub();
            _progression = new ProgressionStub();
            _difficulty = new DifficultyStub();
            _cloud = new CloudStub();
            _analytics = new AnalyticsStub();
            _service = new PostRunService(
                _state, _games, _review, _progression, _difficulty, _cloud, _analytics)
            {
                DayProvider = () => 200,
            };
        }

        private static RunEndInfo Run(long id, float seconds = 120f, int score = 300, int tier = 2, int kills = 3, bool rankUp = false, bool blitz = false)
        {
            var summary = new RunSummary(id, seconds, score, tier, kills, 0);
            var reward = new RunReward(10, 5, 0, 10, 1, rankUp ? 2 : 1, 0);
            return new RunEndInfo(summary, reward, blitz);
        }

        [Test]
        public void OnRunEnded_ReportsLeaderboardsAndAchievementsForAPlausibleRun()
        {
            _service.OnRunEnded(Run(1, seconds: 120f, score: 450, tier: 3, kills: 6));

            Assert.That(_games.Scores[LeaderboardKind.PeakMass], Is.EqualTo(450));
            Assert.That(_games.Scores[LeaderboardKind.KillsPerRun], Is.EqualTo(6));
            Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(6));
            Assert.That(_games.Unlocked, Is.SupersetOf(new[] { AchievementKind.TierPlanet, AchievementKind.TierGasGiant, AchievementKind.Kills5 }));
            Assert.That(_cloud.Uploads, Is.EqualTo(1));
        }

        [Test]
        public void OnRunEnded_EasyDifficultyKeepsAchievementsButSkipsLeaderboards()
        {
            _difficulty.Current = BotDifficulty.Easy;

            _service.OnRunEnded(Run(1, kills: 6));

            Assert.That(_games.Scores, Is.Empty);
            Assert.That(_games.Unlocked, Does.Contain(AchievementKind.Kills5));
        }

        [Test]
        public void OnRunEnded_ImplausibleRunReportsNothing()
        {
            _service.OnRunEnded(Run(1, seconds: 5f, score: 100000, kills: 90));

            Assert.That(_games.Scores, Is.Empty);
            Assert.That(_games.Unlocked, Is.Empty);
            Assert.That(_state.State.RunsFinished, Is.EqualTo(1));
        }

        [Test]
        public void OnRunEnded_AchievementsAreCachedAndReportedOnce()
        {
            _service.OnRunEnded(Run(1, kills: 6));
            int first = _games.Unlocked.Count;

            _service.OnRunEnded(Run(2, kills: 6));

            Assert.That(_games.Unlocked.Count, Is.EqualTo(first));
            Assert.That(_state.State.HasAchievement(AchievementKind.Kills5), Is.True);
        }

        [Test]
        public void OnRunEnded_RankUpAfterEnoughRunsRequestsAReviewOnce()
        {
            _state.State.RunsFinished = ReviewPromptRules.MinRuns;
            _state.State.FirstRunDay = 190;

            _service.OnRunEnded(Run(10, rankUp: true));
            _service.OnRunEnded(Run(11, rankUp: true));

            Assert.That(_review.Requests, Is.EqualTo(1));
            Assert.That(_state.State.ReviewPrompts, Is.EqualTo(1));
            Assert.That(_analytics.Events, Does.Contain(AnalyticsEvents.ReviewPrompt));
        }

        [Test]
        public void OnRunEnded_NoReviewWithoutARankUp()
        {
            _state.State.RunsFinished = 20;
            _state.State.FirstRunDay = 100;

            _service.OnRunEnded(Run(10, rankUp: false));

            Assert.That(_review.Requests, Is.Zero);
        }

    }
}
