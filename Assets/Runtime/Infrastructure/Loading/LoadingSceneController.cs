using System;
using Planet_IO;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Loading
{
    public sealed class LoadingSceneController : IStartable
    {
        private readonly INetworkSessionService _session;

        public LoadingSceneController(INetworkSessionService session)
        {
            _session = session;
        }

        public void Start()
        {
            if (_session.IsServer)
            {
                _ = ContinueToGameAsync();
            }
        }

        private async Awaitable ContinueToGameAsync()
        {
            try
            {
                await _session.ContinueToGameAsync();
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
        }
    }
}
