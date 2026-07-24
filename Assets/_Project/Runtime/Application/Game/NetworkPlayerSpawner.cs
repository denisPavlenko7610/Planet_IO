using System;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.Application
{
    public sealed class NetworkPlayerSpawner : IStartable, IDisposable
    {
        private const float SpawnRadius = 6f;
        private const float SpawnRadiusStep = 2.5f;
        private const float SpawnClearance = 2.5f;
        private const float GoldenAngle = 137.50776f;
        private const int MaximumSpawnAttempts = 24;

        private readonly NetworkManager _networkManager;
        private readonly NetworkObject _playerPrefab;

        public NetworkPlayerSpawner(
            NetworkManager networkManager,
            NetworkObject playerPrefab)
        {
            _networkManager = networkManager
                ?? throw new ArgumentNullException(nameof(networkManager));
            _playerPrefab = playerPrefab
                ? playerPrefab
                : throw new ArgumentNullException(nameof(playerPrefab));
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
                FindSafeSpawnPosition(clientId),
                Quaternion.identity);

            player.SpawnAsPlayerObject(clientId, true);
        }

        private static Vector3 FindSafeSpawnPosition(ulong clientId)
        {
            Vector3 fallback = GetSpawnCandidate(clientId, 0);

            for (int attempt = 0;
                 attempt < MaximumSpawnAttempts;
                 attempt++)
            {
                Vector3 candidate = GetSpawnCandidate(clientId, attempt);
                if (Physics2D.OverlapCircle(
                        candidate,
                        SpawnClearance) == null)
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private static Vector3 GetSpawnCandidate(
            ulong clientId,
            int attempt)
        {
            float angle =
                (clientId + (ulong)attempt) *
                GoldenAngle *
                Mathf.Deg2Rad;
            float radius =
                SpawnRadius +
                attempt / 6 * SpawnRadiusStep;

            return new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f);
        }
    }
}
