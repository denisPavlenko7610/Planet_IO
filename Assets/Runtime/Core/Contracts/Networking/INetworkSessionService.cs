using System;
using UnityEngine;

namespace Planet_IO
{
    public interface INetworkSessionService
    {
        event Action<float> LoadingProgressChanged;
        event Action<NetworkSessionState, string> StateChanged;

        NetworkSessionState State { get; }
        NetworkSessionMode Mode { get; }
        RoomConnectionSettings CurrentRoom { get; }
        string Status { get; }
        float LoadingProgress { get; }
        bool IsServer { get; }
        bool IsSceneEventInProgress { get; }

        Awaitable<bool> StartHostAsync(int maxPlayers);
        Awaitable<bool> StartClientAsync(string relayJoinCode);
        Awaitable<bool> StartSinglePlayerAsync();
        Awaitable ContinueToGameAsync();
        Awaitable ShutdownAndReturnToMenuAsync();
    }
}
