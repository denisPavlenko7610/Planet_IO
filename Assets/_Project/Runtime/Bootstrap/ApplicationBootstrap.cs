using System;
using PlanetIO.Application;
using RD.Foundation;
using RD.SceneFlow;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.Bootstrap
{
    public sealed class ApplicationBootstrap : IStartable, IDisposable
    {
        private readonly ISceneFlow _sceneFlow;
        private readonly IRewardedAdsService _rewardedAdsService;
        private readonly IContentInitializationService _content;
        private readonly PlayerProfileService _profile;
        private readonly ProgressionService _progression;
        private readonly IAnalyticsService _analytics;

        public ApplicationBootstrap(
            IContentInitializationService content,
            ISceneFlow sceneFlow,
            IRewardedAdsService rewardedAdsService,
            PlayerProfileService profile,
            ProgressionService progression,
            IAnalyticsService analytics)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _rewardedAdsService = rewardedAdsService ?? throw new ArgumentNullException(nameof(rewardedAdsService));
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public void Start() => _ = StartAsync();

        private async Awaitable StartAsync()
        {
            try
            {
                await _profile.InitializeAsync();
                await _progression.InitializeAsync();
                _analytics.Track(AnalyticsEvents.SessionStart, ("rank", _progression.Rank), ("streak", _progression.StreakDays));
                await _content.InitializeAsync();
                _rewardedAdsService.Initialize();
                if (_sceneFlow.IsLoaded(SceneIds.Boot))
                {
                    await _sceneFlow.ChangeSceneAsync(SceneIds.Menu);
                }
            }
            catch (Exception exception) { UnityLogger.LogException(exception); }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }

        private async Awaitable StopAsync()
        {
            try
            {
                await _profile.FlushAsync();
                await _progression.FlushAsync();
            }
            catch (Exception exception) { UnityLogger.LogException(exception); }
        }
    }
}
