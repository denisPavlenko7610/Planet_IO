using System;
using UnityEngine;

namespace Planet_IO
{
    public interface INetworkSessionService
    {
        event Action<float> LoadingProgressChanged;
        event Action<NetworkSessionState, string> StateChanged;

        NetworkSessionState State { get; }
        string Status { get; }
        float LoadingProgress { get; }
        bool IsServer { get; }
        bool IsSceneEventInProgress { get; }

        Awaitable<bool> StartHostAsync();
        bool StartClient();
        Awaitable ContinueToGameAsync();
        Awaitable ShutdownAndReturnToMenuAsync();
    }
}
