using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Player
    {
        public override bool IsHarmImmune => IsSpawnProtected;

        public override bool AreMoonsVisible => !IsDefeated;

        protected override bool IsCombatActive => _gameStateService != null && CanActInGameplay;

        protected override ulong AttributionClientId => OwnerClientId;

        protected override void ApplyKnockback(Vector2 velocity)
        {
            ApplyKnockbackRpc(velocity);
        }

        [Rpc(SendTo.Owner)]
        private void ApplyKnockbackRpc(Vector2 velocity)
        {
            ApplyKnockbackLocal(velocity);
        }

        private void AttractFood()
        {
            float reach = FoodMagnetRules.Reach(
                BodyRadius,
                _foodAttractionRadius,
                PerkRules.MagnetRadiusMultiplier(PerkMask));
            int hitCount = Physics2D.OverlapCircle(transform.position, reach, _foodFilter, _foodBuffer);
            float pullStep = _foodAttractionSpeed * Time.deltaTime;

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = _foodBuffer[index];
                if (hit == null ||
                    !hit.TryGetComponent(out Food food) ||
                    food.IsMagnetImmuneFor(NetworkObjectId))
                {
                    continue;
                }

                Transform foodTransform = food.transform;
                Vector2 offset = (Vector2)transform.position - (Vector2)foodTransform.position;
                float distance = offset.magnitude;
                if (distance < 0.01f)
                {
                    continue;
                }

                foodTransform.position += (Vector3)(offset / distance * Mathf.Min(pullStep, distance));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer ||
                !CanActInGameplay ||
                other == null)
            {
                return;
            }

            bool protectedFromHarm = IsSpawnProtected;

            if (other.TryGetComponent(out Player otherPlayer))
            {
                if (!protectedFromHarm)
                {
                    TryEatPlayer(otherPlayer);
                }
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                if (CanDevour(enemy))
                {
                    AbsorbVictim(enemy);
                    NotifyKillRpc(enemy.DisplayName, Constants.CapacityToScore(enemy.Capacity));
                    _enemyRespawnService.Respawn(enemy);
                }
            }
            else if (!protectedFromHarm || other.TryGetComponent<Food>(out _))
            {
                HandleEntityCollision(other);
            }
        }

        private void TryEatPlayer(Player otherPlayer)
        {
            if (otherPlayer == this ||
                otherPlayer.IsDefeated ||
                otherPlayer.IsSpawnProtected ||
                !CanDevour(otherPlayer))
            {
                return;
            }

            if (otherPlayer.TryConsumeAblativeCrust())
            {
                return;
            }

            int score = Constants.CapacityToScore(otherPlayer.Capacity);
            NotifyKillRpc(otherPlayer.DisplayName, score);
            AbsorbVictim(otherPlayer);
            otherPlayer.Defeat(DisplayName, OwnerClientId);
        }

        [Rpc(SendTo.Everyone)]
        private void AnnounceDefeatRpc(FixedString64Bytes killerName, ulong killerClientId)
        {
            DefeatAnnounced?.Invoke(new DefeatAnnouncement(
                killerName.ToString(), killerClientId, DisplayName, OwnerClientId));
        }

        [Rpc(SendTo.Owner)]
        private void NotifyKillRpc(FixedString64Bytes victimName, int score)
        {
            Killed?.Invoke(victimName.ToString(), score);
        }
    }
}
