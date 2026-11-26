using System;
using UnityEngine;

namespace PlanetIO.Application
{
	public sealed class RunCompletionService : IRunCompletionService
	{
		private readonly IProgressionService _progression;
		private readonly IPostRunService _postRun;
		private readonly IAnalyticsService _analytics;

		public RunCompletionService(
			IProgressionService progression,
			IPostRunService postRun,
			IAnalyticsService analytics)
		{
			_progression = progression ?? throw new ArgumentNullException(nameof(progression));
			_postRun = postRun ?? throw new ArgumentNullException(nameof(postRun));
			_analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
		}

		public RunReward Complete(RunSummary summary, int killerTier, RunReward? restoredReward)
		{
			if (restoredReward.HasValue)
			{
				return restoredReward.Value;
			}

			RunReward reward = _progression.ApplyRun(summary);
			_analytics.Track(
				AnalyticsEvents.RunEnd,
				("duration_s", Mathf.RoundToInt(summary.DurationSeconds)),
				("mass_peak", summary.PeakScore),
				("tier_peak", summary.PeakTier),
				("kills", summary.Kills),
				("killer_tier", killerTier),
				("xp", reward.XpGained));
			_postRun.OnRunEnded(new RunEndInfo(summary, reward));

			return reward;
		}
	}
}
