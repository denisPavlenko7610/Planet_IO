using System;

namespace PlanetIO.Application
{
    public sealed class PostRunService : IPostRunService
    {
        private readonly IServiceStateService _stateService;
        private readonly IPlayGamesService _playGames;
        private readonly IReviewService _review;
        private readonly IProgressionService _progression;
        private readonly IRunDifficultyProvider _difficulty;
        private readonly ICloudSyncService _cloud;
        private readonly IAnalyticsService _analytics;

        public PostRunService(
            IServiceStateService stateService,
            IPlayGamesService playGames,
            IReviewService review,
            IProgressionService progression,
            IRunDifficultyProvider difficulty,
            ICloudSyncService cloud,
            IAnalyticsService analytics)
        {
            _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
            _playGames = playGames ?? throw new ArgumentNullException(nameof(playGames));
            _review = review ?? throw new ArgumentNullException(nameof(review));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _difficulty = difficulty ?? throw new ArgumentNullException(nameof(difficulty));
            _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            DayProvider = CurrentUtcDay;
        }

        public Func<int> DayProvider { get; set; }

        public void OnRunEnded(RunEndInfo info)
        {
            if (!_stateService.IsLoaded)
            {
                return;
            }

            ServiceState state = _stateService.State;
            int today = DayProvider();

			bool plausible = ScoreValidationRules.IsPlausible(info.Summary);
			state.RegisterDeath(info.Summary, today, plausible);
			ReportRun(info, state, plausible);
            TryRequestReview(info, state, today);

            _stateService.MarkDirty();
            _cloud.RequestUpload();
        }

		private void ReportRun(RunEndInfo info, ServiceState state, bool plausible)
		{
			if (!plausible)
			{
				return;
			}

			RunSummary run = info.Summary;

            foreach (AchievementKind kind in Enum.GetValues(typeof(AchievementKind)))
            {
                if (AchievementRules.IsEarned(kind, run, _progression.Rank, _progression.StreakDays) && state.AddAchievement(kind))
                {
                    _playGames.Unlock(kind);
                }
            }

            if (run.UsedEasyDifficulty || !ScoreValidationRules.CountsForRanking(_difficulty.Current))
            {
                return;
            }

			state.RegisterRankedRun(run);
            _playGames.ReportScore(LeaderboardKind.PeakMass, run.PeakScore);
            _playGames.ReportScore(LeaderboardKind.KillsPerRun, run.Kills);
			_playGames.ReportScore(LeaderboardKind.TotalKills, state.RankedTotalKills);
        }

        private void TryRequestReview(RunEndInfo info, ServiceState state, int today)
        {
            if (!_review.IsAvailable || !ReviewPromptRules.ShouldPrompt(state, info.Reward.RankedUp, today))
            {
                return;
            }

            state.MarkReviewPrompted(today);
            _analytics.Track(AnalyticsEvents.ReviewPrompt, ("rank", _progression.Rank));
            _review.Request();
        }

        private static int CurrentUtcDay() => (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;
    }
}
