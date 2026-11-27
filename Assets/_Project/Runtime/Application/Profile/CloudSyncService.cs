using System;
using System.Linq;
using UnityEngine;

namespace PlanetIO.Application
{
    public sealed class CloudSyncService : ICloudSyncService, IDisposable
    {
        public const float MinUploadIntervalSeconds = 60f;

        private readonly IPlayGamesService _playGames;
        private readonly IServiceStateService _stateService;
        private readonly ProgressionService _progression;
        private readonly PlayerProfileService _profile;
        private readonly IAnalyticsService _analytics;
		private bool _applyingCloud;
        private bool _busy;
        private bool _synced;
        private bool _uploadPending;
		private bool _flushRequested;
        private float _lastUploadAt = float.NegativeInfinity;

        public CloudSyncService(
            IPlayGamesService playGames,
            IServiceStateService stateService,
            ProgressionService progression,
            PlayerProfileService profile,
            IAnalyticsService analytics)
        {
            _playGames = playGames ?? throw new ArgumentNullException(nameof(playGames));
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
			Clock = () => Time.realtimeSinceStartup;
			_progression.Changed += MarkUploadPending;
			_profile.Changed += MarkUploadPending;
			_stateService.Changed += MarkUploadPending;
        }

		public Func<float> Clock { get; set; }

		public void Dispose()
		{
			_progression.Changed -= MarkUploadPending;
			_profile.Changed -= MarkUploadPending;
			_stateService.Changed -= MarkUploadPending;
		}

		private void MarkUploadPending()
		{
			if (!_applyingCloud)
			{
				_uploadPending = true;
			}
		}

        public void SignIn(bool interactive, Action<bool> completed = null)
        {
            if (!_playGames.IsAvailable)
            {
                completed?.Invoke(false);
                return;
            }

            _playGames.SignIn(interactive, ok =>
            {
                _analytics.Track(AnalyticsEvents.SignIn, ("ok", ok ? 1 : 0), ("auto", interactive ? 0 : 1));
                if (ok)
                {
                    SyncNow();
                }

                completed?.Invoke(ok);
            });
        }

		public void RequestUpload()
		{
			_uploadPending = true;
			if (!_synced || Clock() - _lastUploadAt >= MinUploadIntervalSeconds)
			{
				SyncNow();
			}
		}

		public void FlushPending()
		{
			if (!_uploadPending || !_playGames.IsSignedIn)
			{
				return;
			}

			if (_busy)
			{
				_flushRequested = true;
				return;
			}

			SyncNow();
		}

		public void SyncNow()
		{
			if (!_playGames.IsSignedIn || _busy || !_stateService.IsLoaded)
			{
				return;
			}

			_busy = true;
			string outcome = null;
			bool writing = false;
			_playGames.SyncCloud(bytes =>
			{
				CloudSaveData cloud = CloudSaveRules.Deserialize(bytes);
				CloudMergeResult result = CloudSaveRules.Merge(Capture(), cloud);
				if (result.Data == null)
				{
					return null;
				}

				if (result.ChangesLocal)
				{
					_applyingCloud = true;
					try
					{
						Apply(result.Data);
					}
					finally
					{
						_applyingCloud = false;
					}
				}

				CloudSaveData merged = Capture();
				if (!_synced || result.ChangesLocal)
				{
					PushPlatformData();
				}

				_synced = true;
				_uploadPending = false;
				writing = result.ChangesCloud || merged.ToJson() != result.Data.ToJson();
				outcome = result.ChangesLocal
					? (writing ? "merged" : "downloaded")
					: (writing ? "uploaded" : "same");

				return writing ? CloudSaveRules.Serialize(merged) : null;
			}, TimeSpan.FromSeconds(Mathf.Max(0f, Clock())), ok =>
			{
				if (writing)
				{
					_lastUploadAt = Clock();
				}

				bool synced = ok && outcome != null;
				if (!synced)
				{
					_uploadPending = true;
				}

				Finish(synced ? outcome : "failed", synced);
			});
		}

        private void Finish(string result, bool ok)
        {
            _busy = false;
			bool flush = _flushRequested;
			_flushRequested = false;
            _analytics.Track(AnalyticsEvents.CloudSync, ("result", result), ("ok", ok ? 1 : 0));
			if (ok && flush)
			{
				FlushPending();
			}
        }

        private CloudSaveData Capture()
        {
            ServiceState state = _stateService.State;
            var data = new CloudSaveData
            {
                Progression = _progression.ExportState(),
                TotalKills = state.TotalKills,
				RankedTotalKills = state.RankedTotalKills,
				RankedBestScore = state.RankedBestScore,
                Achievements = state.Achievements.ToArray()
            };
            _profile.ExportCloud(data);
            return data;
        }

        private void Apply(CloudSaveData data)
        {
            ServiceState state = _stateService.State;
            _progression.ImportState(data.Progression);
            _profile.ImportCloud(data);
            state.TotalKills = Math.Max(state.TotalKills, data.TotalKills);
			state.RankedTotalKills = Math.Max(state.RankedTotalKills, data.RankedTotalKills);
			state.RankedBestScore = Math.Max(state.RankedBestScore, data.RankedBestScore);
            state.MergeAchievements(data.Achievements);
            _stateService.MarkDirty();
        }

        private void PushPlatformData()
        {
            ServiceState state = _stateService.State;
            foreach (AchievementKind kind in Enum.GetValues(typeof(AchievementKind)))
            {
                if (state.HasAchievement(kind))
                {
                    _playGames.Unlock(kind);
                }
            }

			_playGames.ReportScore(LeaderboardKind.TotalKills, state.RankedTotalKills);
			if (state.RankedBestScore <= ScoreValidationRules.MaxScore)
            {
				_playGames.ReportScore(LeaderboardKind.PeakMass, state.RankedBestScore);
            }
        }
    }
}
