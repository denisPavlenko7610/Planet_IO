using System;
using RD.Foundation;
using System.IO;
using RD.Save;
using RD.AddressableAssets;
using RD.Attributes;
using PlanetIO.Application;
using PlanetIO.Infrastructure.Ads;
using PlanetIO.Infrastructure.Analytics;
using PlanetIO.Infrastructure.Audio;
using PlanetIO.Infrastructure.Networking;
using PlanetIO.Infrastructure.Loading;
using PlanetIO.Infrastructure.Localization;
using PlanetIO.Infrastructure.PlayGames;
using PlanetIO.Infrastructure.Review;
using PlanetIO.Infrastructure.Services;
using PlanetIO.Infrastructure.Settings;
using RD.Localization;
using RD.Settings;
using PlanetIO.Infrastructure.Mobile;
using Unity.Netcode;
using RD.SceneFlow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlanetIO.Bootstrap
{
    [DefaultExecutionOrder(-6000)]
    [DisallowMultipleComponent]
    public sealed partial class ApplicationLifetimeScope : LifetimeScope
    {
        private static ApplicationLifetimeScope _activeScope;

        [SerializeField, Assign] private NetworkManager _networkManager;
        [SerializeField, Assign] private SceneCatalog _sceneCatalog;
        [SerializeField] private SfxCatalog _sfxCatalog;
        [SerializeField] private PlanetSkinCatalog _skinCatalog;

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

            if (_sfxCatalog == null)
            {
                throw new MissingComponentException($"{nameof(ApplicationLifetimeScope)} requires a {nameof(SfxCatalog)}.");
            }

            if (_skinCatalog == null)
            {
                throw new MissingComponentException($"{nameof(ApplicationLifetimeScope)} requires a {nameof(PlanetSkinCatalog)}.");
            }

            builder.RegisterComponent(_networkManager);
            builder.Register<MatchMigrationStore>(Lifetime.Singleton);
            builder.RegisterInstance(_sceneCatalog);
            builder.Register<SceneFlow>(resolver => new SceneFlow(
                    resolver.Resolve<SceneCatalog>()),
                Lifetime.Singleton)
                .As<ISceneFlow>();
            builder.Register<SaveStore>(_ => new SaveStore(
#if UNITY_EDITOR || PLANET_IO_MIGRATION_PROBE
                HostMigrationPeerProbe.StoragePath(Path.Combine(UnityEngine.Application.persistentDataPath, "Saves"))
#else
                Path.Combine(UnityEngine.Application.persistentDataPath, "Saves")
#endif
                ), Lifetime.Singleton);
            builder.Register<PlayerProfileService>(Lifetime.Singleton).AsSelf().As<IPlayerProfileService>();
            builder.Register<LocalAnalyticsService>(Lifetime.Singleton).AsSelf().As<IAnalyticsService>();
            builder.Register<ProgressionService>(Lifetime.Singleton).AsSelf().As<IProgressionService>();
            builder.Register<AddressableService>(Lifetime.Singleton);
            builder.Register<ISettingsStore>(_ => new PlayerPrefsSettingsStore(GameSettingKeys.StorePrefix), Lifetime.Singleton);
            builder.Register<SettingsService>(Lifetime.Singleton).As<ISettingsService>();
            builder.RegisterEntryPoint<GameSettingsApplier>();
            builder.Register<UnityLocalizationProvider>(Lifetime.Singleton);
            builder.Register<ILocalizationService>(resolver =>
            {
                UnityLocalizationProvider provider = resolver.Resolve<UnityLocalizationProvider>();
                provider.Initialize();
                return new LocalizationService(provider);
            }, Lifetime.Singleton);
            builder.Register<SettingsRoomPreferences>(Lifetime.Singleton).As<IRoomPreferences>();
            builder.Register<AddressableContentService>(Lifetime.Singleton).As<IContentInitializationService>();
            builder.RegisterInstance(_sfxCatalog);
            builder.RegisterInstance(_skinCatalog);
            builder.Register<SfxPlayer>(Lifetime.Singleton).As<ISfxPlayer>();
            builder.Register<AdMobRewardedService>(Lifetime.Singleton).AsSelf().As<IRewardedAdsService>().As<IAdPrivacyService>();
            builder.Register<ServiceStateService>(Lifetime.Singleton).As<IServiceStateService>();
            builder.Register<PlayGamesService>(Lifetime.Singleton).As<IPlayGamesService>();
            builder.Register<PlayReviewService>(Lifetime.Singleton).As<IReviewService>();
            builder.Register<RunDifficultyProvider>(Lifetime.Singleton).As<IRunDifficultyProvider>();
            builder.Register<CloudSyncService>(Lifetime.Singleton).As<ICloudSyncService>();
            builder.Register<PostRunService>(Lifetime.Singleton).As<IPostRunService>();
            builder.RegisterEntryPoint<AddressableMusicService>();
			builder.RegisterEntryPoint<NetworkSessionService>()
                .AsSelf()
                .As<INetworkSessionService>();

            builder.RegisterEntryPoint<MobileRuntimeService>();
            builder.RegisterEntryPoint<ApplicationBootstrap>();
        }

        private async void OnApplicationPause(bool paused)
        {
            if (!paused || Container == null) return;
            try
            {
                await Container.Resolve<PlayerProfileService>().FlushAsync();
                await Container.Resolve<ProgressionService>().FlushAsync();
                await Container.Resolve<IServiceStateService>().FlushAsync();
                Container.Resolve<ICloudSyncService>().FlushPending();
            }
            catch (Exception exception) { UnityLogger.LogException(exception); }
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
