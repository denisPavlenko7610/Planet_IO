using System;
using UnityEngine;
using UnityTemplates.SceneFlow;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Boot
{
    public sealed class ApplicationBootstrap : IStartable
    {
        private readonly IContentInitializationService _contentInitializationService;
        private readonly ISceneFlow _sceneFlow;

        public ApplicationBootstrap(
            IContentInitializationService contentInitializationService,
            ISceneFlow sceneFlow)
        {
            _contentInitializationService = contentInitializationService ?? throw new ArgumentNullException(nameof(contentInitializationService));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
        }

        public void Start()
        {
            if (_sceneFlow.IsLoaded(SceneIds.Boot))
            {
                _ = LoadMenuAsync();
            }
        }

        private async Awaitable LoadMenuAsync()
        {
            try
            {
                await _contentInitializationService.InitializeAsync();
                if (!_contentInitializationService.IsReady)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
                await _sceneFlow.ChangeSceneAsync(SceneIds.Menu);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
            }
        }
    }
}
