using System;
using Planet_IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Boot
{
    public sealed class ApplicationBootstrap : IStartable
    {
        public void Start()
        {
            if (SceneManager.GetActiveScene().name == SceneNames.Boot)
            {
                _ = LoadMenuAsync();
            }
        }

        private static async Awaitable LoadMenuAsync()
        {
            try
            {
                await Awaitable.NextFrameAsync(Application.exitCancellationToken);
                await SceneManager.LoadSceneAsync(SceneNames.Menu, LoadSceneMode.Single);
            }
            catch (OperationCanceledException)
            {
                // Application is closing.
            }
        }
    }
}
