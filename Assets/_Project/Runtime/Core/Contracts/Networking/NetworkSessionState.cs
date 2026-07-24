namespace PlanetIO
{
    public enum NetworkSessionState : byte
    {
        Offline,
        StartingHost,
        StartingClient,
        StartingSinglePlayer,
        Connecting,
        Loading,
        InGame,
        ShuttingDown,
        Failed
    }
}
