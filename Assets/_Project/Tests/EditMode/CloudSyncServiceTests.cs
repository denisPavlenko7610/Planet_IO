using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PlanetIO.Application;
using RD.Save;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
	public sealed class CloudSyncServiceTests
	{
		private const string CatalogPath = "Assets/_Project/ScriptableObjects/Skins/PlanetSkins.asset";

		private FakePlayGames _games;
		private FakeServiceState _state;
		private FakeAnalytics _analytics;
		private ProgressionService _progression;
		private PlayerProfileService _profile;
		private CloudSyncService _sync;
		private float _now;

		[UnitySetUp]
		public IEnumerator SetUp() => Run(CreateAsync());

		private async Awaitable CreateAsync()
		{
			SaveStore saves = new(new MemoryStorage(), new UnityJsonSaveSerializer());
			_games = new FakePlayGames();
			_state = new FakeServiceState();
			_analytics = new FakeAnalytics();
			_progression = new ProgressionService(saves, _analytics) { DayProvider = () => 100 };
			await _progression.InitializeAsync();
			_profile = new PlayerProfileService(AssetDatabase.LoadAssetAtPath<PlanetSkinCatalog>(CatalogPath), saves);
			await _profile.InitializeAsync();
			_now = 0f;
			_sync = new CloudSyncService(_games, _state, _progression, _profile, _analytics) { Clock = () => _now };
		}

		private static IEnumerator Run(Awaitable operation)
		{
			var awaiter = operation.GetAwaiter();
			while (!awaiter.IsCompleted) yield return null;
			awaiter.GetResult();
		}

		private void EarnProgress() => _progression.ApplyRun(new RunSummary(1, 120f, 300, 2, 3, 0));

		private static byte[] CloudBytes(int xp, string nickname)
		{
			var data = new CloudSaveData
			{
				Progression = new ProgressionState { Xp = xp, LastLoginDay = 100 },
				Nickname = nickname,
				SelectedSkin = string.Empty
			};
			return CloudSaveRules.Serialize(data);
		}

		[UnityTest]
		public IEnumerator RequestUpload_WhenSignedOut_DoesNotTouchTheCloud()
		{
			_games.IsSignedIn = false;

			_sync.RequestUpload();
			_sync.FlushPending();

			Assert.That(_games.Loads, Is.Zero);
			Assert.That(_games.Saves, Is.Zero);
			yield break;
		}

		[UnityTest]
		public IEnumerator RequestUpload_BeforeTheFirstSync_LoadsTheCloudThenUploadsLocalProgress()
		{
			EarnProgress();

			_sync.RequestUpload();

			Assert.That(_games.Loads, Is.EqualTo(1));
			Assert.That(_games.Saves, Is.EqualTo(1));
			CloudSaveData uploaded = CloudSaveRules.Deserialize(_games.LastSaved);
			Assert.That(uploaded.IsValid(), Is.True);
			Assert.That(uploaded.Progression.Xp, Is.EqualTo(_progression.Xp));
			Assert.That(uploaded.Progression.Xp, Is.GreaterThan(0));
			yield break;
		}

		[UnityTest]
		public IEnumerator FailedLoad_NeverOverwritesTheCloud()
		{
			EarnProgress();
			_games.LoadStatus = CloudLoadStatus.Failed;

			_sync.RequestUpload();

			Assert.That(_games.Loads, Is.EqualTo(1));
			Assert.That(_games.Saves, Is.Zero);
			Assert.That(_analytics.Events.Any(e => e.Name == AnalyticsEvents.CloudSync && e.Detail.Contains("result=failed")), Is.True);
			yield break;
		}

		[UnityTest]
		public IEnumerator Uploads_AreRateLimitedAndFlushIgnoresTheInterval()
		{
			_sync.RequestUpload();
			Assert.That(_games.Saves, Is.EqualTo(1));

			_now = CloudSyncService.MinUploadIntervalSeconds - 1f;
			_sync.RequestUpload();
			Assert.That(_games.Saves, Is.EqualTo(1));

			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(2));

			_now += CloudSyncService.MinUploadIntervalSeconds;
			_sync.RequestUpload();
			Assert.That(_games.Saves, Is.EqualTo(3));
			yield break;
		}

		[UnityTest]
		public IEnumerator FlushPending_WithNothingRequested_DoesNotUpload()
		{
			_sync.FlushPending();

			Assert.That(_games.Loads, Is.Zero);
			Assert.That(_games.Saves, Is.Zero);
			yield break;
		}

		[UnityTest]
		public IEnumerator FailedUpload_StaysPendingUntilTheNextFlush()
		{
			_sync.RequestUpload();
			int afterFirst = _games.Saves;
			_games.SaveResult = false;
			_now = CloudSyncService.MinUploadIntervalSeconds * 2f;

			_sync.RequestUpload();
			Assert.That(_games.Saves, Is.EqualTo(afterFirst + 1));

			_games.SaveResult = true;
			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(afterFirst + 2));

			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(afterFirst + 2));
			yield break;
		}

		[UnityTest]
		public IEnumerator CloudAhead_ReplacesLocalProgressionAndNickname()
		{
			_games.LoadBytes = CloudBytes(5000, "CloudNick");

			_sync.SyncNow();

			Assert.That(_progression.Xp, Is.EqualTo(5000));
			Assert.That(_profile.Nickname, Is.EqualTo("CloudNick"));
			Assert.That(_state.DirtyCount, Is.GreaterThan(0));
			yield break;
		}

		[UnityTest]
		public IEnumerator LocalAhead_KeepsLocalProgressionAndUploadsIt()
		{
			EarnProgress();
			int localXp = _progression.Xp;
			string localNickname = _profile.Nickname;
			_games.LoadBytes = CloudBytes(1, "CloudNick");

			_sync.SyncNow();

			Assert.That(_progression.Xp, Is.EqualTo(localXp));
			Assert.That(_profile.Nickname, Is.EqualTo(localNickname));
			Assert.That(_games.Saves, Is.EqualTo(1));
			Assert.That(CloudSaveRules.Deserialize(_games.LastSaved).Progression.Xp, Is.EqualTo(localXp));
			yield break;
		}

		[UnityTest]
		public IEnumerator SignIn_WhenUnavailable_CompletesFalseWithoutSyncing()
		{
			_games.IsAvailable = false;
			bool? result = null;

			_sync.SignIn(true, ok => result = ok);

			Assert.That(result, Is.False);
			Assert.That(_games.Loads, Is.Zero);
			yield break;
		}

		[UnityTest]
		public IEnumerator SignIn_Success_SyncsOnceAndReportsTheOutcome()
		{
			bool? result = null;

			_sync.SignIn(false, ok => result = ok);

			Assert.That(result, Is.True);
			Assert.That(_games.Loads, Is.EqualTo(1));
			Assert.That(
				_analytics.Events.Any(e =>
					e.Name == AnalyticsEvents.SignIn && e.Detail.Contains("ok=1") && e.Detail.Contains("auto=1")
				),
				Is.True
			);
			yield break;
		}

		[UnityTest]
		public IEnumerator RequestDuringUpload_RemainsPendingUntilFlushed()
		{
			_games.DeferSaves = true;
			_sync.RequestUpload();
			_profile.SetNickname("NewNickname");
			_sync.RequestUpload();
			_games.CompleteSave(true);
			Assert.That(_games.Saves, Is.EqualTo(1), "Normal uploads retain the rate limit.");

			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(2));
			Assert.That(CloudSaveRules.Deserialize(_games.LastSaved).Nickname, Is.EqualTo("NewNickname"));
			_games.CompleteSave(true);
			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(2));
			yield break;
		}

		[UnityTest]
		public IEnumerator PauseDuringUpload_FlushesNewSnapshotAfterCompletion()
		{
			_games.DeferSaves = true;
			_sync.RequestUpload();
			_profile.SetNickname("PauseNickname");
			_sync.RequestUpload();
			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(1));

			_games.CompleteSave(true);
			Assert.That(_games.Saves, Is.EqualTo(2));
			Assert.That(CloudSaveRules.Deserialize(_games.LastSaved).Nickname, Is.EqualTo("PauseNickname"));
			_games.CompleteSave(true);
			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(2));
			yield break;
		}

		[UnityTest]
		public IEnumerator PauseDuringFailedUpload_RetainsPendingWorkWithoutImmediateRetry()
		{
			_games.DeferSaves = true;
			_sync.RequestUpload();
			_sync.RequestUpload();
			_sync.FlushPending();
			_games.CompleteSave(false);
			Assert.That(_games.Saves, Is.EqualTo(1));

			_sync.FlushPending();
			Assert.That(_games.Saves, Is.EqualTo(2));
			_games.CompleteSave(true);
			yield break;
		}

		[UnityTest]
		public IEnumerator Sync_ReportsOnlyQualifiedRankedRecordsAndPreservesPersonalRecords()
		{
			_state.State.TotalKills = 100;
			_profile.SubmitScore(700);
			_sync.SyncNow();
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.Zero);
			Assert.That(_games.Scores[LeaderboardKind.PeakMass], Is.Zero);
			Assert.That(_profile.BestScore, Is.EqualTo(700));

			_state.State.RankedTotalKills = 3;
			_state.State.RankedBestScore = 300;
			_sync.SyncNow();
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(3));
			Assert.That(_games.Scores[LeaderboardKind.PeakMass], Is.EqualTo(300));
			CloudSaveData saved = CloudSaveRules.Deserialize(_games.LastSaved);
			Assert.That(saved.RankedTotalKills, Is.EqualTo(3));
			Assert.That(saved.RankedBestScore, Is.EqualTo(300));
			Assert.That(saved.BestScore, Is.EqualTo(700));
			yield break;
		}

		[UnityTest]
		public IEnumerator Sync_RestoresQualifiedRecordsFromCloud()
		{
			CloudSaveData data = CloudSaveRules.Deserialize(CloudBytes(5000, "CloudNick"));
			data.BestScore = 700;
			data.TotalKills = 100;
			data.RankedTotalKills = 4;
			data.RankedBestScore = 350;
			_games.LoadBytes = CloudSaveRules.Serialize(data);
			_sync.SyncNow();
			Assert.That(_state.State.RankedTotalKills, Is.EqualTo(4));
			Assert.That(_state.State.RankedBestScore, Is.EqualTo(350));
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(4));
			Assert.That(_games.Scores[LeaderboardKind.PeakMass], Is.EqualTo(350));
			yield break;
		}

		private sealed class MemoryStorage : ISaveStorage
		{
			private readonly Dictionary<string, string> _slots = new();

			public async Awaitable<bool> ExistsAsync(string slotId, CancellationToken token = default)
			{
				await System.Threading.Tasks.Task.Delay(1, token);
				return _slots.ContainsKey(slotId);
			}

			public async Awaitable<string> ReadAsync(string slotId, CancellationToken token = default)
			{
				await System.Threading.Tasks.Task.Delay(1, token);
				return _slots.TryGetValue(slotId, out string payload) ? payload : throw new FileNotFoundException();
			}

			public async Awaitable WriteAsync(string slotId, string payload, CancellationToken token = default)
			{
				await System.Threading.Tasks.Task.Delay(1, token);
				_slots[slotId] = payload;
			}

			public async Awaitable DeleteAsync(string slotId, CancellationToken token = default)
			{
				await System.Threading.Tasks.Task.Delay(1, token);
				_slots.Remove(slotId);
			}
		}
	}
}
