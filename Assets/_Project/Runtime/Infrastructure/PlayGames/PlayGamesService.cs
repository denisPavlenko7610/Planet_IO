using System;
using PlanetIO.Infrastructure.Services;
using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

namespace PlanetIO.Infrastructure.PlayGames
{
    public sealed class PlayGamesService : IPlayGamesService
    {
        public event Action StateChanged;

        private bool _activated;
        private bool _signedIn;
        private bool _signingIn;

        public bool IsAvailable => PlatformServiceIds.IsPlayableDevice && HasAppId();

        public bool IsSignedIn => _signedIn;

        public void SignIn(bool interactive, Action<bool> completed)
        {
#if UNITY_ANDROID
            if (!IsAvailable || _signingIn)
            {
                completed?.Invoke(_signedIn);
                return;
            }

            Activate();
            _signingIn = true;
            Action<SignInStatus> handler = status =>
            {
                _signingIn = false;
                SetSignedIn(status == SignInStatus.Success);
                completed?.Invoke(_signedIn);
            };

            if (interactive)
            {
                PlayGamesPlatform.Instance.ManuallyAuthenticate(handler);
            }
            else
            {
                PlayGamesPlatform.Instance.Authenticate(handler);
            }
#else
            completed?.Invoke(false);
#endif
        }

        public void ReportScore(LeaderboardKind board, long score)
        {
#if UNITY_ANDROID
            string id = PlatformServiceIds.LeaderboardId(board);
            if (!_signedIn || string.IsNullOrEmpty(id) || score <= 0)
            {
                return;
            }

            PlayGamesPlatform.Instance.ReportScore(score, id, success =>
            {
                if (!success)
                {
                    Debug.LogWarning($"Play Games score report failed: {board}");
                }
            });
#endif
        }

        public void Unlock(AchievementKind achievement)
        {
#if UNITY_ANDROID
            string id = PlatformServiceIds.AchievementId(achievement);
            if (!_signedIn || string.IsNullOrEmpty(id))
            {
                return;
            }

            PlayGamesPlatform.Instance.ReportProgress(id, 100.0, success =>
            {
                if (!success)
                {
                    Debug.LogWarning($"Play Games achievement report failed: {achievement}");
                }
            });
#endif
        }

        public void ShowLeaderboards()
        {
#if UNITY_ANDROID
            if (_signedIn)
            {
                PlayGamesPlatform.Instance.ShowLeaderboardUI();
            }
#endif
        }

        public void ShowAchievements()
        {
#if UNITY_ANDROID
            if (_signedIn)
            {
                PlayGamesPlatform.Instance.ShowAchievementsUI();
            }
#endif
        }

		public void SyncCloud(Func<byte[], byte[]> merge, TimeSpan playedTime, Action<bool> completed)
		{
#if UNITY_ANDROID
			if (!_signedIn)
			{
				completed(false);
				return;
			}

			ISavedGameClient client = PlayGamesPlatform.Instance.SavedGame;
			SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
				.WithUpdatedPlayedTime(playedTime)
				.WithUpdatedDescription("Planet IO progress")
				.Build();
			client.OpenWithManualConflictResolution(
				PlatformServiceIds.CloudSnapshotName,
				DataSource.ReadNetworkOnly,
				true,
				(resolver, original, originalBytes, unmerged, unmergedBytes) =>
				{
					CloudMergeResult result = CloudSaveRules.Merge(
						CloudSaveRules.Deserialize(originalBytes), CloudSaveRules.Deserialize(unmergedBytes));
					if (result.Data == null || !result.ChangesLocal)
					{
						resolver.ChooseMetadata(original);
						return;
					}

					resolver.ResolveConflict(original, update, CloudSaveRules.Serialize(result.Data));
				},
				(openStatus, metadata) =>
				{
					if (openStatus != SavedGameRequestStatus.Success)
					{
						completed(false);
						return;
					}

					client.ReadBinaryData(metadata, (readStatus, bytes) =>
					{
						if (readStatus != SavedGameRequestStatus.Success)
						{
							completed(false);
							return;
						}

						byte[] merged = merge(bytes);
						if (merged == null)
						{
							completed(true);
							return;
						}

						client.CommitUpdate(metadata, update, merged,
							(commitStatus, _) => completed(commitStatus == SavedGameRequestStatus.Success));
					});
				});
#else
			completed(false);
#endif
		}

#if UNITY_ANDROID
        private void Activate()
        {
            if (_activated)
            {
                return;
            }

            PlayGamesPlatform.Activate();
            _activated = true;
        }
#endif

        private void SetSignedIn(bool value)
        {
            if (_signedIn == value)
            {
                return;
            }

            _signedIn = value;
            StateChanged?.Invoke();
        }

        private static bool HasAppId()
        {
#if UNITY_ANDROID
            PlayGamesSettings settings = PlayGamesSettings.LoadInstance();
            return settings != null && !string.IsNullOrEmpty(settings.AppId);
#else
            return false;
#endif
        }
    }
}
