using UnityEngine;

namespace PlanetIO
{
	public sealed partial class EnemyMovement
	{
		private PlanetScale _raidTarget;
		private float _raidRemaining;
		private float _raidCooldown;
		private float _raidInitialMass;

		private void ResetMoonRaid()
		{
			_raidTarget = null;
			_raidRemaining = 0f;
			_raidCooldown = MoonRaidRules.CooldownSeconds;
		}

		private void UpdateMoonRaid(float deltaTime)
		{
			_raidCooldown = Mathf.Max(0f, _raidCooldown - deltaTime);
			if (_raidTarget == null)
			{
				return;
			}

			_raidRemaining -= deltaTime;
			if (!_raidTarget.IsSpawned ||
				_raidTarget.IsHiddenFromRivals ||
				_raidTarget is Player { IsDefeated: true } or Player { IsSpawnProtected: true } ||
				MoonRaidRules.ShouldRetreat(_raidRemaining, _raidInitialMass, _enemy.Capacity, _enemy.MoonMask != 0))
			{
				EndMoonRaid();
			}
		}

		private void EndMoonRaid()
		{
			Vector2 threat = _raidTarget != null
				? _raidTarget.transform.position
				: transform.position - (Vector3)Direction;

			ResetMoonRaid();
			EvadeFrom(threat);
		}

		private bool TryMoonRaid(PlanetScale target, bool hazard)
		{
			bool permitted = target != null &&
				MoonRaidRules.CanRaid(
					_enemy.Capacity,
					target.Capacity,
					_enemy.MoonMask != 0,
					Difficulty,
					Personality == BotPersonality.Cautious,
					hazard,
					0f
				);
			if (_raidTarget != null && (!permitted || target != _raidTarget))
			{
				EndMoonRaid();
				return true;
			}
			if (!permitted || (_raidTarget == null && _raidCooldown > 0f))
			{
				return false;
			}

			if (_raidTarget == null)
			{
				_raidTarget = target;
				_raidRemaining = MoonRaidRules.CommitmentSeconds;
				_raidInitialMass = _enemy.Capacity;
			}

			Vector2 center = transform.position;
			Vector2 targetCenter = target.transform.position;
			Vector2 toTarget = (targetCenter - center).normalized;
			Vector2 heading = toTarget;
			float best = float.NegativeInfinity;
			for (int index = 0; index < _enemy.MoonSlots; index++)
			{
				if ((_enemy.MoonMask & (1 << index)) == 0)
				{
					continue;
				}
				Vector2 offset = MoonRules.Position(
						center,
						_enemy.BodyRadius,
						index,
						_enemy.MoonSlots,
						OrbitClock.Now,
						_enemy.MoonPhaseDegrees
					) -
					center;
				float alignment = Vector2.Dot(offset.normalized, toTarget);
				if (alignment <= best)
				{
					continue;
				}
				best = alignment;
				heading = offset.normalized;
			}
			float standOff = MoonRaidRules.StandOff(_enemy.BodyRadius, target.BodyRadius);
			if (Vector2.Distance(center, targetCenter) <
				_enemy.BodyRadius + target.BodyRadius + _enemy.BodyRadius * 0.1f)
			{
				EndMoonRaid();
				return true;
			}
			SetTarget(targetCenter - heading * standOff, MovementState.Hunting);
			return true;
		}
	}
}