using NUnit.Framework;
using PlanetIO.Application;

namespace PlanetIO.Tests
{
	public sealed class PostRunServiceTotalKillsTests
	{
		private FakeServiceState _state;
		private FakePlayGames _games;
		private PostRunService _service;
		private FakeDifficulty _difficulty;

		[SetUp]
		public void SetUp()
		{
			_state = new FakeServiceState();
			_games = new FakePlayGames();
			_difficulty = new FakeDifficulty();
			_service = new PostRunService(
				_state,
				_games,
				new FakeReview(),
				new FakeProgression(),
				_difficulty,
				new FakeCloudSync(),
				new FakeAnalytics())
			{
				DayProvider = () => 200
			};
		}

		private static RunEndInfo Run(long id, float seconds, int score, int kills) => new(
			new RunSummary(id, seconds, score, 2, kills, 0),
			new RunReward(10, 5, 0, 10, 1, 1, 0)
		);

		[Test]
		public void RankedScores_ExcludeEasyRunsAndLegacyPersonalRecords()
		{
			_state.State.TotalKills = 100;
			_difficulty.Current = BotDifficulty.Easy;
			_service.OnRunEnded(Run(1, 120f, 700, 6));
			Assert.That(_games.Scores, Is.Empty);
			Assert.That(_state.State.RankedBestScore, Is.Zero);

			_difficulty.Current = BotDifficulty.Medium;
			_service.OnRunEnded(Run(2, 120f, 300, 1));

			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(1));
			Assert.That(_state.State.RankedBestScore, Is.EqualTo(300));
			Assert.That(_state.State.TotalKills, Is.EqualTo(107));
		}

		[Test]
		public void RankedScores_ExcludeEarlierEasyExposureAfterSwitchingToMedium()
		{
			_difficulty.Current = BotDifficulty.Medium;
			RunEndInfo result = new(
				new RunSummary(1, 120f, 300, 2, 3, 0, true),
				new RunReward(10, 5, 0, 10, 1, 1, 0)
			);

			_service.OnRunEnded(result);

			Assert.That(_games.Scores, Is.Empty);
			Assert.That(_state.State.RankedTotalKills, Is.Zero);
			Assert.That(_state.State.TotalKills, Is.EqualTo(3));
		}

		[Test]
		public void TotalKillsBoard_AccumulatesEachRunOnceAndOnlyTheContinueDelta()
		{
			_service.OnRunEnded(Run(1, 120f, 300, 3));
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(3));

			_service.OnRunEnded(Run(2, 120f, 300, 4));
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(7));

			_service.OnRunEnded(Run(2, 180f, 300, 6));
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(9));

			_service.OnRunEnded(Run(2, 180f, 300, 6));
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(9));
		}

		[Test]
		public void TotalKillsBoard_IgnoresKillsOfAnImplausibleRun()
		{
			_service.OnRunEnded(Run(1, 5f, 100, 90));
			_service.OnRunEnded(Run(2, 120f, 300, 3));

			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(3));
		}

		[Test]
		public void TotalKillsBoard_CountsAllKillsOnceTheSameRunBecomesPlausible()
		{
			_service.OnRunEnded(Run(1, 8f, 100, 2));
			Assert.That(_state.State.TotalKills, Is.Zero);

			_service.OnRunEnded(Run(1, 60f, 300, 4));

			Assert.That(_state.State.TotalKills, Is.EqualTo(4));
			Assert.That(_games.Scores[LeaderboardKind.TotalKills], Is.EqualTo(4));
			Assert.That(_state.State.RunsFinished, Is.EqualTo(1));
		}
	}
}
