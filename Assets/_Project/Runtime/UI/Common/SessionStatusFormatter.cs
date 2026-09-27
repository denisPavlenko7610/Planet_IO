using UnityTemplates.Localization;

namespace PlanetIO.UI
{
    public static class SessionStatusFormatter
    {
        public static string Format(ILocalizationService localization, NetworkSessionState state, NetworkSessionMode mode, string roomCode,
            SessionFailure failure = SessionFailure.None)
        {
            bool hasRoomCode = !string.IsNullOrEmpty(roomCode);
            return state switch
            {
                NetworkSessionState.StartingHost => hasRoomCode
                    ? localization.Get(LocalizationKeys.StatusRoomCreated, roomCode)
                    : localization.Get(LocalizationKeys.StatusCreatingRoom),
                NetworkSessionState.StartingClient or NetworkSessionState.Connecting => mode == NetworkSessionMode.SinglePlayer
                    ? localization.Get(LocalizationKeys.StatusStartingSolo)
                    : localization.Get(LocalizationKeys.StatusConnecting, roomCode),
                NetworkSessionState.StartingSinglePlayer => localization.Get(LocalizationKeys.StatusStartingSolo),
                NetworkSessionState.Loading or NetworkSessionState.InGame => localization.Get(LocalizationKeys.StatusLoading),
                NetworkSessionState.ShuttingDown => localization.Get(LocalizationKeys.StatusLeaving),
                NetworkSessionState.Failed => failure switch
                {
                    SessionFailure.HostLeft => localization.Get(LocalizationKeys.StatusHostLeft),
                    SessionFailure.RoomFull => localization.Get(LocalizationKeys.StatusRoomFull),
                    SessionFailure.VersionMismatch => localization.Get(LocalizationKeys.StatusVersionMismatch),
                    _ => localization.Get(LocalizationKeys.StatusFailed)
                },
                _ => localization.Get(LocalizationKeys.StatusReady)
            };
        }

        public static bool IsError(NetworkSessionState state) => state == NetworkSessionState.Failed;
    }
}
