using System;
using UnityEngine;
using UnityTemplates.SceneFlow;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Bootstrap
{
    public sealed class ApplicationBootstrap : IStartable
    {
        private readonly IContentInitializationService _contentInitializationService;
        private readonly ISceneFlow _sceneFlow;
        private readonly IRewardedAdsService _rewardedAdsService;

        public ApplicationBootstrap(
            IContentInitializationService contentInitializationService,
            ISceneFlow sceneFlow,
            IRewardedAdsService rewardedAdsService)
        {
            _contentInitializationService = contentInitializationService ?? throw new ArgumentNullException(nameof(contentInitializationService));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _rewardedAdsService = rewardedAdsService ?? throw new ArgumentNullException(nameof(rewardedAdsService));
        }

        public void Start()
        {
            _rewardedAdsService.Initialize();

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
                    GameLogger.LogError("Content initialization failed. Menu will not load.");
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
                GameLogger.LogException(exception);
            }
        }
    }
}
