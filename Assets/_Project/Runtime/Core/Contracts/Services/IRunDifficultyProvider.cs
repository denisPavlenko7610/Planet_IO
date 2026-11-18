namespace PlanetIO
{
    public interface IRunDifficultyProvider
    {
        BotDifficulty Current { get; }
    }
}
