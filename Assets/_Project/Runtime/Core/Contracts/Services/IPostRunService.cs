namespace PlanetIO
{
    public readonly struct RunEndInfo
    {
        public readonly RunSummary Summary;
        public readonly RunReward Reward;
        public readonly bool IsBlitz;

        public RunEndInfo(RunSummary summary, RunReward reward, bool isBlitz)
        {
            Summary = summary;
            Reward = reward;
            IsBlitz = isBlitz;
        }
    }

    public interface IPostRunService
    {
        void OnRunEnded(RunEndInfo info);
    }
}
