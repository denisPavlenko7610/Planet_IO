using PlanetIO.Core.Attributes;
using Planet_IO;
using PlanetIO.Infrastructure.Boot;
using PlanetIO.Infrastructure.Networking;
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
        public static ApplicationLifetimeScope Instance { get; private set; }

        [SerializeField, Assign] private NetworkManager _networkManager;

        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _networkManager ??= GetComponent<NetworkManager>();
            DontDestroyOnLoad(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            if (_networkManager == null)
            {
                throw new MissingComponentException(
                    $"{nameof(ApplicationLifetimeScope)} requires {nameof(NetworkManager)} on the same GameObject.");
            }

            builder.RegisterComponent(_networkManager);
            builder.RegisterEntryPoint<NetworkSessionService>()
                .AsSelf()
                .As<INetworkSessionService>();
            builder.RegisterEntryPoint<ApplicationBootstrap>();
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroy();
        }
    }
}
