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

        public void LoadCloud(Action<CloudLoadStatus, byte[]> completed)
        {
#if UNITY_ANDROID
            if (!_signedIn)
            {
                completed?.Invoke(CloudLoadStatus.Failed, null);
                return;
            }

            ISavedGameClient client = PlayGamesPlatform.Instance.SavedGame;
            client.OpenWithAutomaticConflictResolution(
                PlatformServiceIds.CloudSnapshotName,
                DataSource.ReadNetworkOnly,
                ConflictResolutionStrategy.UseMostRecentlySaved,
                (openStatus, metadata) =>
                {
                    if (openStatus != SavedGameRequestStatus.Success)
                    {
                        completed?.Invoke(CloudLoadStatus.Failed, null);
                        return;
                    }

                    client.ReadBinaryData(metadata, (readStatus, bytes) =>
                    {
                        completed?.Invoke(
                            readStatus == SavedGameRequestStatus.Success ? CloudLoadStatus.Loaded : CloudLoadStatus.Failed,
                            bytes);
                    });
                });
#else
            completed?.Invoke(CloudLoadStatus.Failed, null);
#endif
        }

        public void SaveCloud(byte[] data, TimeSpan playedTime, Action<bool> completed)
        {
#if UNITY_ANDROID
            if (!_signedIn || data == null || data.Length == 0)
            {
                completed?.Invoke(false);
                return;
            }

            ISavedGameClient client = PlayGamesPlatform.Instance.SavedGame;
            client.OpenWithAutomaticConflictResolution(
                PlatformServiceIds.CloudSnapshotName,
                DataSource.ReadNetworkOnly,
                ConflictResolutionStrategy.UseMostRecentlySaved,
                (openStatus, metadata) =>
                {
                    if (openStatus != SavedGameRequestStatus.Success)
                    {
                        completed?.Invoke(false);
                        return;
                    }

                    SavedGameMetadataUpdate update = new SavedGameMetadataUpdate.Builder()
                        .WithUpdatedPlayedTime(playedTime)
                        .WithUpdatedDescription("Planet IO progress")
                        .Build();
                    client.CommitUpdate(metadata, update, data,
                        (commitStatus, _) => completed?.Invoke(commitStatus == SavedGameRequestStatus.Success));
                });
#else
            completed?.Invoke(false);
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
