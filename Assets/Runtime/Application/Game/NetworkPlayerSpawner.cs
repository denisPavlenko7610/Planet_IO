using System;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Planet_IO.Application
{
    public sealed class NetworkPlayerSpawner : IStartable, IDisposable
    {
        private const float SpawnRadius = 6f;
        private const float GoldenAngle = 137.50776f;

        private readonly NetworkManager _networkManager;
        private readonly NetworkObject _playerPrefab;

        public NetworkPlayerSpawner(
            NetworkManager networkManager,
            NetworkObject playerPrefab)
        {
            _networkManager = networkManager;
            _playerPrefab = playerPrefab;
        }

        public void Start()
        {
            if (!_networkManager.IsServer)
            {
                return;
            }

            _networkManager.OnClientConnectedCallback += SpawnPlayer;

            foreach (NetworkClient client in _networkManager.ConnectedClientsList)
            {
                SpawnPlayer(client.ClientId);
            }
        }

        public void Dispose()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= SpawnPlayer;
            }
        }

        private void SpawnPlayer(ulong clientId)
        {
            if (!_networkManager.IsServer ||
                _playerPrefab == null ||
                !_networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
                client.PlayerObject != null)
            {
                return;
            }

            NetworkObject player = Object.Instantiate(
                _playerPrefab,
                GetSpawnPosition(clientId),
                Quaternion.identity);

            player.SpawnAsPlayerObject(clientId, true);
        }

        private static Vector3 GetSpawnPosition(ulong clientId)
        {
            float angle = clientId * GoldenAngle * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(angle) * SpawnRadius,
                Mathf.Sin(angle) * SpawnRadius,
                0f);
        }
    }
}
