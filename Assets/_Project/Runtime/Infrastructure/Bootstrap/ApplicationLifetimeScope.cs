using System;
using PlanetIO.Core.Attributes;
using PlanetIO.Application;
using PlanetIO.Infrastructure.Ads;
using PlanetIO.Infrastructure.Bootstrap;
using PlanetIO.Infrastructure.Audio;
using PlanetIO.Infrastructure.Networking;
using PlanetIO.Infrastructure.Loading;
using PlanetIO.Infrastructure.Mobile;
using Unity.Netcode;
using UnityTemplates.SceneFlow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlanetIO.Infrastructure.Bootstrap
{
    [DefaultExecutionOrder(-6000)]
    [DisallowMultipleComponent]
    public sealed class ApplicationLifetimeScope : LifetimeScope
    {
        private static ApplicationLifetimeScope _activeScope;

        [SerializeField, Assign] private NetworkManager _networkManager;
        [SerializeField, Assign] private SceneCatalog _sceneCatalog;

        protected override void Awake()
        {
            if (_activeScope != null && _activeScope != this)
            {
                Destroy(gameObject);
                return;
            }

            _activeScope = this;
            _networkManager ??= GetComponent<NetworkManager>();
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (_networkManager == null)
            {
                throw new MissingComponentException($"{nameof(ApplicationLifetimeScope)} requires {nameof(NetworkManager)} on the same GameObject.");
            }

            if (_sceneCatalog == null)
            {
                throw new InvalidOperationException($"{nameof(ApplicationLifetimeScope)} requires a {nameof(SceneCatalog)}.");
            }

            builder.RegisterComponent(_networkManager);
            builder.RegisterInstance(_sceneCatalog);
            builder.Register<SceneFlow>(resolver => new SceneFlow(
                    resolver.Resolve<SceneCatalog>()),
                Lifetime.Singleton)
                .As<ISceneFlow>();
            builder.Register<PlayerProfileService>(Lifetime.Singleton).As<IPlayerProfileService>();
            builder.Register<PlayerPrefsRoomPreferences>(Lifetime.Singleton).As<IRoomPreferences>();
            builder.Register<AddressableContentService>(Lifetime.Singleton).As<IContentInitializationService>();
            builder.Register<AdMobRewardedService>(Lifetime.Singleton).As<IRewardedAdsService>();
            builder.RegisterEntryPoint<AddressableMusicService>();
			builder.RegisterEntryPoint<NetworkSessionService>()
                .AsSelf()
                .As<INetworkSessionService>();

            builder.RegisterEntryPoint<MobileRuntimeService>();
            builder.RegisterEntryPoint<ApplicationBootstrap>();
        }

        protected override void OnDestroy()
        {
            if (_activeScope == this)
            {
                _activeScope = null;
            }

            base.OnDestroy();
        }
    }
}
