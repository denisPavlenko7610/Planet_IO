using System;
using PlanetIO;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Loading
{
    public sealed class LoadingSceneController : IStartable
    {
        private readonly INetworkSessionService _networkSessionService;

        public LoadingSceneController(INetworkSessionService networkSessionService)
        {
            _networkSessionService = networkSessionService
                ?? throw new ArgumentNullException(nameof(networkSessionService));
        }

        public void Start()
        {
            if (_networkSessionService.IsServer)
            {
                _ = ContinueToGameAsync();
            }
        }

        private async Awaitable ContinueToGameAsync()
        {
            try
            {
                await _networkSessionService.ContinueToGameAsync();
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
        }
    }
}
