namespace Planet_IO
{
    public enum NetworkSessionState : byte
    {
        Offline,
        StartingHost,
        StartingClient,
        Connecting,
        Loading,
        InGame,
        ShuttingDown,
        Failed
    }
}
