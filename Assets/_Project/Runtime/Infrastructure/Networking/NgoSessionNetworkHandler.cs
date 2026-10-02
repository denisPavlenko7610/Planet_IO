using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace PlanetIO.Infrastructure.Networking
{
    public sealed class NgoSessionNetworkHandler : INetworkHandler
    {
        private const float ClientConnectionTimeoutSeconds = 10f;

        private readonly NetworkManager _networkManager;
        private readonly Action<bool> _beforeStart;

        public NgoSessionNetworkHandler(NetworkManager networkManager, Action<bool> beforeStart)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _beforeStart = beforeStart ?? throw new ArgumentNullException(nameof(beforeStart));
        }

        public bool IsHost { get; private set; }

        public async Task StartAsync(NetworkConfiguration configuration)
        {
            if (_networkManager.NetworkConfig.NetworkTransport is not UnityTransport transport)
            {
                throw new InvalidOperationException("Sessions require Unity Transport.");
            }

            IsHost = configuration.Role != NetworkRole.Client;
            transport.SetRelayServerData(IsHost ? configuration.RelayServerData : configuration.RelayClientData);
            _beforeStart(IsHost);

            if (IsHost)
            {
                if (!_networkManager.StartHost())
                {
                    throw new InvalidOperationException("NetworkManager rejected host start.");
                }

                return;
            }

            if (!_networkManager.StartClient())
            {
                throw new InvalidOperationException("NetworkManager rejected client start.");
            }

            float deadline = Time.realtimeSinceStartup + ClientConnectionTimeoutSeconds;
            while (!_networkManager.IsConnectedClient)
            {
                if (!_networkManager.IsListening || Time.realtimeSinceStartup > deadline)
                {
                    string reason = string.IsNullOrWhiteSpace(_networkManager.DisconnectReason)
                        ? "Room did not respond."
                        : _networkManager.DisconnectReason;
                    throw new InvalidOperationException(reason);
                }

                await Task.Yield();
            }
        }

        public Task StopAsync()
        {
            if (_networkManager.IsListening && !_networkManager.ShutdownInProgress)
            {
                _networkManager.Shutdown();
            }

            return Task.CompletedTask;
        }
    }
}
