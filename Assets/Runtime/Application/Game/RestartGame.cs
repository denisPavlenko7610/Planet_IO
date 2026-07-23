using System;
using UnityEngine;
using VContainer;

namespace Planet_IO.Application
{
    public sealed class RestartGame : MonoBehaviour
    {
        private INetworkSessionService _networkSessionService;
        private IGameStateService _gameStateService;

        [Inject]
        public void Construct(
            INetworkSessionService networkSessionService,
            IGameStateService gameStateService)
        {
            _networkSessionService = networkSessionService
                ?? throw new ArgumentNullException(nameof(networkSessionService));
            _gameStateService = gameStateService
                ?? throw new ArgumentNullException(nameof(gameStateService));
        }

        public void Restart()
        {
            _ = RestartAsync();
        }

        private async Awaitable RestartAsync()
        {
            if (_networkSessionService == null)
            {
                Debug.LogError($"{nameof(RestartGame)} has no network session service.", this);
                return;
            }

            _gameStateService?.BeginShutdown();

            try
            {
                await _networkSessionService.ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
                // The application is closing.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
