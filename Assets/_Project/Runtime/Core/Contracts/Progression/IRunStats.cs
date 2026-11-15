namespace PlanetIO
{
    public interface IRunStats
    {
        RunSummary Current { get; }
        string KillerName { get; }
        int KillerTier { get; }

        void BeginRun();
    }
}
