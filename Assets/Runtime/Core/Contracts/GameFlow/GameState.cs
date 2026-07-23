namespace Planet_IO
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
