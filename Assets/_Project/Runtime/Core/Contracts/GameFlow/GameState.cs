namespace PlanetIO
{
    public enum GameState : byte
    {
        None,
        Initializing,
        WaitingForPlayers,
        Playing,
        GameOver,
        ShuttingDown
    }
}
