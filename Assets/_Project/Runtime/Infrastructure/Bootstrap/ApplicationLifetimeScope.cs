using System;
using PlanetIO.Core.Attributes;
using PlanetIO.Application;
using PlanetIO.Infrastructure.Boot;
using PlanetIO.Infrastructure.Audio;
using PlanetIO.Infrastructure.Networking;
using PlanetIO.Infrastructure.Loading;
using PlanetIO.Infrastructure.Mobile;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace PlanetIO.Infrastructure
{
    [DefaultExecutionOrder(-6000)]
    [DisallowMultipleComponent]
    public sealed class ApplicationLifetimeScope : LifetimeScope
    {
        private static ApplicationLifetimeScope _activeScope;

        [SerializeField, Assign] private NetworkManager _networkManager;

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

            builder.RegisterComponent(_networkManager);
            builder.Register<PlayerProfileService>(Lifetime.Singleton).As<IPlayerProfileService>();
            builder.Register<PlayerPrefsRoomPreferences>(Lifetime.Singleton).As<IRoomPreferences>();
            builder.Register<AddressableContentService>(Lifetime.Singleton).As<IContentInitializationService>();
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
