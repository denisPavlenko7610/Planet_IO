namespace PlanetIO
{
    public readonly struct RunEndInfo
    {
        public readonly RunSummary Summary;
        public readonly RunReward Reward;

		public RunEndInfo(RunSummary summary, RunReward reward)
        {
            Summary = summary;
            Reward = reward;
        }
    }

    public interface IPostRunService
    {
        void OnRunEnded(RunEndInfo info);
    }
}
