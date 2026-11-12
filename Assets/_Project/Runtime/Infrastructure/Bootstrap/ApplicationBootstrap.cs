using System;
using System.Collections.Generic;
using System.Threading;
using PlanetIO.Application;
using RD.Foundation;
using RD.SceneFlow;
using RD.Startup;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Bootstrap
{
    public sealed class ApplicationBootstrap : IStartable, IDisposable
    {
        private readonly ISceneFlow _sceneFlow;
        private readonly IRewardedAdsService _rewardedAdsService;
        private readonly StartupPipeline _startup;
        private readonly CancellationTokenSource _lifetime = new();
        private bool _starting;

        public ApplicationBootstrap(
            IContentInitializationService content,
            ISceneFlow sceneFlow,
            IRewardedAdsService rewardedAdsService,
            PlayerProfileService profile)
        {
            _sceneFlow = sceneFlow ?? throw new ArgumentNullException(nameof(sceneFlow));
            _rewardedAdsService = rewardedAdsService ?? throw new ArgumentNullException(nameof(rewardedAdsService));
            _startup = new StartupPipeline(new IStartupStep[]
            {
                new ProfileStep(profile), new ContentStep(content)
            });
        }

        public void Start() => _ = StartAsync();

        private async Awaitable StartAsync()
        {
            _starting = true;
            try
            {
                await _startup.StartAsync(_lifetime.Token);
                _lifetime.Token.ThrowIfCancellationRequested();
                _rewardedAdsService.Initialize();
                if (_sceneFlow.IsLoaded(SceneIds.Boot))
				{
					await _sceneFlow.ChangeSceneAsync(SceneIds.Menu, _lifetime.Token);
				}
			}
            catch (OperationCanceledException) { }
            catch (Exception exception) { UnityLogger.LogException(exception); }
            finally { _starting = false; }
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _ = StopAsync();
        }

        private async Awaitable StopAsync()
        {
            try
            {
                while (_starting) await Awaitable.NextFrameAsync();
                await _startup.StopAsync();
            }
            catch (Exception exception) { UnityLogger.LogException(exception); }
            finally { _lifetime.Dispose(); }
        }

        private sealed class ProfileStep : StartupStep
        {
            private readonly PlayerProfileService _profile;
            public ProfileStep(PlayerProfileService profile) : base("profile") => _profile = profile;
            public override Awaitable StartAsync(CancellationToken token = default) => _profile.InitializeAsync(token);
            public override Awaitable StopAsync(CancellationToken token = default) => _profile.FlushAsync();
        }

        private sealed class ContentStep : StartupStep
        {
            private static readonly string[] RequiredSteps = { "profile" };
            private readonly IContentInitializationService _content;
            public ContentStep(IContentInitializationService content) : base("content") => _content = content;
            public override IReadOnlyList<string> Dependencies => RequiredSteps;
            public override async Awaitable StartAsync(CancellationToken token = default)
            {
                token.ThrowIfCancellationRequested();
                await _content.InitializeAsync();
                token.ThrowIfCancellationRequested();
            }
        }
    }
}
