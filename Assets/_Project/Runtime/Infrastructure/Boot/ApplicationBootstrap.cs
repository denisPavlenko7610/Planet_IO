using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Boot
{
    public sealed class ApplicationBootstrap : IStartable
    {
        private readonly IContentInitializationService _contentInitializationService;

        public ApplicationBootstrap(IContentInitializationService contentInitializationService)
        {
            _contentInitializationService = contentInitializationService ?? throw new ArgumentNullException(nameof(contentInitializationService));
        }

        public void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneNames.Boot)
            {
                _ = LoadMenuAsync();
            }
        }

        private async Awaitable LoadMenuAsync()
        {
            try
            {
                await _contentInitializationService.InitializeAsync();
                await Awaitable.NextFrameAsync();
                await SceneManager.LoadSceneAsync(SceneNames.Menu, LoadSceneMode.Single);
            }
            catch (OperationCanceledException)
            {
				LoggerIO.LogError("Application is closing");
            }
        }
    }
}
