using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Player
    {
        protected override void DeathCheck(float capacity)
        {
            if (IsServer && capacity <= MinCapacity)
            {
                if (TryConsumeAblativeCrust(true))
                {
                    return;
                }

                Defeat(LastAttackerName, LastAttackerClientId);
            }
        }

        public void Defeat(string killerName = null, ulong killerClientId = Constants.UnassignedClientId)
        {
            if (!IsServer || IsDefeated)
            {
                return;
            }

            if (!string.IsNullOrEmpty(killerName))
            {
                AnnounceDefeatRpc(killerName, killerClientId);
            }

            ResetBoost();
            _capacityAtDefeat = Capacity;
            _lootDrop = _lootSpawnService?.SpawnLoot(transform.position, Capacity);

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            ResetMoons();
            _networkDefeated.Value = true;
        }

        private void OnDefeatedChanged(bool _, bool isDefeated)
        {
            SetBodyActive(!isDefeated);

            if (isDefeated)
            {
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.linearVelocity = Vector2.zero;
                }

                GameVfx.Death(transform.position, _networkColor.Value, Capacity);

                if (IsOwner)
                {
                    Defeated?.Invoke();
                }

                return;
            }

            if (IsOwner)
            {
                TeleportLocal(PickRespawnPosition());
                Revived?.Invoke();
            }
        }

        private void SetBodyActive(bool active)
        {
            if (TryGetComponent(out Collider2D bodyCollider))
            {
                bodyCollider.enabled = active;
            }

            if (TryGetComponent(out SpriteRenderer bodyRenderer))
            {
                bodyRenderer.enabled = active;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ContinueRpc()
        {
            if (!IsServer ||
                !IsDefeated ||
                _continueUsedThisLife)
            {
                return;
            }

            _continueUsedThisLife = true;
            _lootDrop?.Discard();
            _lootDrop = null;
            ResetBoost();
            Revive(_continueProtectionTime, Mathf.Max(InitialCapacity, _capacityAtDefeat * _continueMassFraction));
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RespawnRpc()
        {
            if (!IsServer ||
                !IsDefeated)
            {
                return;
            }

            _continueUsedThisLife = false;
            _lootDrop = null;
            ResetBoost();
            Revive(_spawnInvincibilityTime, InitialCapacity);
            ResetPerksForNewLife();
        }

        private void Revive(float protectionTime, float capacity)
        {
            _invincibilityTimeRemaining = protectionTime;
            Capacity = capacity;
            _networkDefeated.Value = false;
            RefreshMoonSlots();
        }
    }
}
