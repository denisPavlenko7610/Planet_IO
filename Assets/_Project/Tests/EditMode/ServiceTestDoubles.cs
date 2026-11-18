using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace PlanetIO.Tests
{
	internal sealed class FakeServiceState : IServiceStateService
	{
		public event Action Changed;
		public bool IsLoaded => true;
		public ServiceState State { get; } = new();
		public int DirtyCount;

		public Awaitable InitializeAsync(CancellationToken cancellationToken = default) => default;

		public void MarkDirty()
		{
			DirtyCount++;
			Changed?.Invoke();
		}

		public Awaitable FlushAsync() => default;
	}

	internal sealed class FakePlayGames : IPlayGamesService
	{
		public event Action StateChanged;
		public bool IsAvailable { get; set; } = true;
		public bool IsSignedIn { get; set; } = true;
		public CloudLoadStatus LoadStatus = CloudLoadStatus.Loaded;
		public byte[] LoadBytes;
		public bool SaveResult = true;
		public int Loads;
		public int Saves;
		public byte[] LastSaved;
		public bool DeferSaves;
		public Action<bool> CompleteSave;
		public readonly Dictionary<LeaderboardKind, long> Scores = new();
		public readonly List<AchievementKind> Unlocked = new();

		public void SignIn(bool interactive, Action<bool> completed) => completed?.Invoke(IsSignedIn);

		public void ReportScore(LeaderboardKind board, long score) => Scores[board] = score;

		public void Unlock(AchievementKind achievement) => Unlocked.Add(achievement);

		public void ShowLeaderboards()
		{
		}

		public void ShowAchievements()
		{
		}

		public void LoadCloud(Action<CloudLoadStatus, byte[]> completed)
		{
			Loads++;
			completed?.Invoke(LoadStatus, LoadBytes);
		}

		public void SaveCloud(byte[] data, TimeSpan playedTime, Action<bool> completed)
		{
			Saves++;
			LastSaved = data;
			if (DeferSaves)
				CompleteSave = completed;
			else
			completed?.Invoke(SaveResult);
		}

		public void RaiseStateChanged() => StateChanged?.Invoke();
	}

	internal sealed class FakeAnalytics : IAnalyticsService
	{
		public readonly List<(string Name, string Detail)> Events = new();

		public void Track(string eventName, params (string Key, object Value)[] parameters) =>
			Events.Add((eventName, string.Join(",", (parameters ?? Array.Empty<(string Key, object Value)>()).Select(p => p.Key + "=" + p.Value))));
	}

	internal sealed class FakeReview : IReviewService
	{
		public bool IsAvailable { get; set; } = true;
		public int Requests;

		public void Request() => Requests++;
	}

	internal sealed class FakeDifficulty : IRunDifficultyProvider
	{
		public BotDifficulty Current { get; set; } = BotDifficulty.Medium;
	}

	internal sealed class FakeCloudSync : ICloudSyncService
	{
		public int Uploads;

		public void SignIn(bool interactive, Action<bool> completed = null) => completed?.Invoke(true);

		public void SyncNow()
		{
		}

		public void RequestUpload() => Uploads++;

		public void FlushPending()
		{
		}
	}

	internal sealed class FakeProgression : IProgressionService
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
	}
