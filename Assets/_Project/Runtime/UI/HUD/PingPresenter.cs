using System;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class PingPresenter : IStartable, ITickable
    {
        private const float RefreshIntervalSeconds = 1f;
        private const float SmoothingFactor = 0.5f;
        private const int GoodThresholdMs = 80;
        private const int FairThresholdMs = 160;
        private const int MaxDisplayedMs = 999;

        private readonly NetworkManager _networkManager;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ISessionHudView _view;
        private float _refreshTimeRemaining;
        private float _smoothedMs = -1f;

        public PingPresenter(
            NetworkManager networkManager,
            INetworkSessionService networkSessionService,
            ISessionHudView view)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Start()
        {
            Hide();
        }

        public void Tick()
        {
            _refreshTimeRemaining -= Time.unscaledDeltaTime;
            if (_refreshTimeRemaining > 0f)
            {
                return;
            }

            _refreshTimeRemaining = RefreshIntervalSeconds;
            Refresh();
        }

        private void Refresh()
        {
            if (_networkSessionService.Mode != NetworkSessionMode.Client
                || !_networkManager.IsConnectedClient
                || _networkManager.NetworkConfig?.NetworkTransport == null)
            {
                Hide();
                return;
            }

            float rtt = _networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
            _smoothedMs = _smoothedMs < 0f ? rtt : Mathf.Lerp(_smoothedMs, rtt, SmoothingFactor);

            int milliseconds = Mathf.Min(Mathf.RoundToInt(_smoothedMs), MaxDisplayedMs);
            PingQuality quality = milliseconds <= GoodThresholdMs
                ? PingQuality.Good
                : milliseconds <= FairThresholdMs ? PingQuality.Fair : PingQuality.Poor;

            _view.SetPing(milliseconds, quality);
        }

        private void Hide()
        {
            _smoothedMs = -1f;
            _view.SetPing(-1, PingQuality.Hidden);
        }
    }
}
