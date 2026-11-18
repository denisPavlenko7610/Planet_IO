using System;

namespace PlanetIO
{
    public enum CloudLoadStatus
    {
        Loaded,
        Failed
    }

    public interface IPlayGamesService
    {
        event Action StateChanged;

        bool IsAvailable { get; }
        bool IsSignedIn { get; }

        void SignIn(bool interactive, Action<bool> completed);
        void ReportScore(LeaderboardKind board, long score);
        void Unlock(AchievementKind achievement);
        void ShowLeaderboards();
        void ShowAchievements();
        void LoadCloud(Action<CloudLoadStatus, byte[]> completed);
        void SaveCloud(byte[] data, TimeSpan playedTime, Action<bool> completed);
    }
}
