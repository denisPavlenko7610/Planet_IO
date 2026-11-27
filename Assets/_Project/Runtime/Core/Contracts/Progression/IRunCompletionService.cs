namespace PlanetIO
{
	public interface IRunCompletionService
	{
		RunReward Complete(RunSummary summary, int killerTier);
	}
}
