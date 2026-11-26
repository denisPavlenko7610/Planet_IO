using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO.Application
{
    public sealed partial class NetworkWorldReadyState
    {
        private MatchMigrationStore _migration;
        private readonly NetworkVariable<bool> _handoffPaused = new(false);
        private readonly Dictionary<ulong, (Vector3 Position, Quaternion Rotation)> _acknowledged = new();
        [Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;

        private void PauseChanged(bool oldValue, bool value)
        {
            if (value)
            {
                _migration?.BeginRecovery();
            }
        }

        private async Awaitable PrepareHandoffAsync()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            _acknowledged.Clear();
            ulong[] peers = NetworkManager.ConnectedClientsIds
                .Where(id => id != Unity.Netcode.NetworkManager.ServerClientId).ToArray();
            _handoffPaused.Value = true;
            PauseForHandoffRpc();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (Time.realtimeSinceStartup < deadline &&
                peers.Any(id => NetworkManager.ConnectedClients.ContainsKey(id) && !_acknowledged.ContainsKey(id)))
            {
                await Awaitable.NextFrameAsync();
            }
            foreach (var pair in _acknowledged)
            {
                if (NetworkManager.ConnectedClients.TryGetValue(pair.Key, out var client) &&
                    client.PlayerObject != null)
                {
                    client.PlayerObject.transform.SetPositionAndRotation(pair.Value.Position, pair.Value.Rotation);
                    if (client.PlayerObject.TryGetComponent(out Rigidbody2D body))
                    {
                        body.position = pair.Value.Position;
                        body.rotation = pair.Value.Rotation.eulerAngles.z;
                    }
                }
            }
        }

        [Rpc(SendTo.NotServer)] private void PauseForHandoffRpc()
        {
            _migration?.BeginRecovery();
            var player = NetworkManager.LocalClient?.PlayerObject;
            AcknowledgeHandoffRpc(
                player != null ? player.transform.position : Vector3.zero,
                player != null ? player.transform.rotation : Quaternion.identity
            );
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void AcknowledgeHandoffRpc(Vector3 position, Quaternion rotation, RpcParams rpc = default)
        {
            if (!_handoffPaused.Value ||
                !IsServer ||
                !float.IsFinite(position.x) ||
                !float.IsFinite(position.y) ||
                !float.IsFinite(position.z) ||
                !float.IsFinite(rotation.x) ||
                !float.IsFinite(rotation.y) ||
                !float.IsFinite(rotation.z) ||
                !float.IsFinite(rotation.w))
            {
                return;
            }

            _acknowledged[rpc.Receive.SenderClientId] = (Constants.ClampToWorld(position), rotation);
        }
    }
}
