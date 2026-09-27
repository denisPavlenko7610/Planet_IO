namespace PlanetIO
{
    public enum SessionFailure : byte
    {
        None,
        ConnectionFailed,
        HostLeft,
        RoomFull,
        VersionMismatch
    }

    public static class SessionFailureReasons
    {
        public const string InvalidPayload = "Invalid connection payload.";
        public const string VersionMismatch = "Client version does not match room version.";
        public const string RoomFull = "Room is full.";
        public const string SinglePlayerOnly = "This session is running in single player mode.";

        public static SessionFailure FromDisconnectReason(string reason, bool wasClient)
        {
            return reason switch
            {
                RoomFull => SessionFailure.RoomFull,
                VersionMismatch => SessionFailure.VersionMismatch,
                _ when wasClient && string.IsNullOrWhiteSpace(reason) => SessionFailure.HostLeft,
                _ => SessionFailure.ConnectionFailed
            };
        }
    }
}
