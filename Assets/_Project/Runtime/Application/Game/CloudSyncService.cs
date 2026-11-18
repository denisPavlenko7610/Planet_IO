using System;
using System.Linq;
using UnityEngine;

namespace PlanetIO.Application
{
    public sealed class CloudSyncService : ICloudSyncService
    {
        public const float MinUploadIntervalSeconds = 60f;

        private readonly IPlayGamesService _playGames;
        private readonly IServiceStateService _stateService;
        private readonly ProgressionService _progression;
        private readonly PlayerProfileService _profile;
        private readonly IAnalyticsService _analytics;
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
        }

        public Func<float> Clock { get; set; }

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

        public void SyncNow()
        {
            if (!_playGames.IsSignedIn || _busy || !_stateService.IsLoaded)
            {
                return;
            }

            _busy = true;
            _playGames.LoadCloud(OnLoaded);
        }

        public void RequestUpload()
        {
            _uploadPending = true;
            if (!_playGames.IsSignedIn || _busy)
            {
                return;
            }

            if (!_synced)
            {
                SyncNow();
                return;
            }

            if (Clock() - _lastUploadAt >= MinUploadIntervalSeconds)
            {
                UploadCurrent();
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

            if (!_synced)
            {
                SyncNow();
                return;
            }

            UploadCurrent();
        }

        private void OnLoaded(CloudLoadStatus status, byte[] bytes)
        {
            if (status != CloudLoadStatus.Loaded)
            {
                Finish("failed", false);
                return;
            }

            CloudSaveData cloud = CloudSaveRules.Deserialize(bytes);
            CloudMergeResult result = CloudSaveRules.Merge(Capture(), cloud);
            if (result.Data == null)
            {
                Finish("failed", false);
                return;
            }

            if (result.ChangesLocal)
            {
                Apply(result.Data);
            }

            PushPlatformData();
            _synced = true;
            if (result.ChangesCloud)
            {
                Upload(result.Data, result.ChangesLocal ? "merged" : "uploaded");
                return;
            }

            Finish(result.ChangesLocal ? "downloaded" : "same", true);
        }

        private void UploadCurrent()
        {
            _busy = true;
            Upload(Capture(), "uploaded");
        }

		private void Upload(CloudSaveData data, string result)
		{
			byte[] bytes = CloudSaveRules.Serialize(data);
			_uploadPending = false;
			_playGames.SaveCloud(bytes, TimeSpan.FromSeconds(Mathf.Max(0f, Clock())), ok =>
			{
				_lastUploadAt = Clock();
				if (!ok)
				{
					_uploadPending = true;
				}

				Finish(ok ? result : "failed", ok);
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
