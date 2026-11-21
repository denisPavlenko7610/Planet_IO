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
			PullFoodTowardsBody(
				_foodAttractionRadius,
				PerkRules.MagnetRadiusMultiplier(PerkMask),
				_foodAttractionSpeed,
				Time.deltaTime);
        }

		protected override void ResolvePlanetContact(PlanetScale other)
		{
			if (!CanActInGameplay)
			{
				return;
			}

			if (other is Player otherPlayer)
			{
				if (!IsSpawnProtected)
				{
					TryEatPlayer(otherPlayer);
				}
			}
			else if (other is Enemy enemy)
			{
				TryEatEnemy(enemy);
			}
		}

		private void TryEatEnemy(Enemy enemy)
		{
			if (!CanDevour(enemy))
			{
				return;
			}

			AbsorbVictim(enemy);
			NotifyKillRpc(enemy.DisplayName, Constants.CapacityToScore(enemy.Capacity));
			_enemyRespawnService.Respawn(enemy);
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
				TryEatEnemy(enemy);
            }
            else if (!protectedFromHarm)
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
