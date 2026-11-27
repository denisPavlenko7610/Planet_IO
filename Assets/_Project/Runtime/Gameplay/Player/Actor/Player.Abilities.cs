using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Player
    {
        private const int TrailCapacity = 24;
        private const int TrailContactBuffer = 8;
        private const float TrailPointInterval = 0.1f;
        private const float TrailStrikeInterval = 0.12f;
        private const float TrailMinimumRadius = 0.3f;
        private const float TrailRadiusFactor = 0.45f;

        private readonly NetworkVariable<bool> _networkStealth = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly Vector2[] _trailPositions = new Vector2[TrailCapacity];
        private readonly float[] _trailRemaining = new float[TrailCapacity];
        private readonly Collider2D[] _trailContacts = new Collider2D[TrailContactBuffer];

        private ContactFilter2D _trailFilter;
        private int _trailNext;
        private float _trailSpawnTimer;
        private float _trailStrikeTimer;
        private float _stealthRemaining;
        private float _stealthCooldown;

        public bool IsStealthed => IsSpawned && _networkStealth.Value;

        public float TrailRadius => Mathf.Max(TrailMinimumRadius, BodyRadius * TrailRadiusFactor);

        public override bool IsHiddenFromRivals => IsStealthed || base.IsHiddenFromRivals;

        public override void Teleport(Vector2 position)
        {
            TeleportRpc(position);
        }

        [Rpc(SendTo.Owner)]
        private void TeleportRpc(Vector2 position)
        {
            TeleportLocal(position);
        }

        private void InitializeAbilities()
        {
            _trailFilter = ContactFilter2D.noFilter;
            _trailFilter.SetLayerMask(~LayerMask.GetMask(Constants.FoodLayerName));
        }

        private void AttachAbilityViews()
		{
			EnsureComponent<IonTrailView>();
		}

        private void UpdateAbilities(float deltaTime)
        {
            UpdateStealth(deltaTime);
            UpdateIonTrail(deltaTime);
        }

        private void OnBoostEnded()
        {
            if (!IsServer || !HasPerk(PerkId.NebulaVeil) || _stealthCooldown > 0f)
            {
                return;
            }

            _stealthRemaining = PerkRules.StealthSeconds;
            _stealthCooldown = PerkRules.StealthCooldownSeconds;
        }

        private void UpdateStealth(float deltaTime)
        {
            if (_stealthCooldown > 0f)
            {
                _stealthCooldown -= deltaTime;
            }

            bool active = _stealthRemaining > 0f && !IsDefeated && HasPerk(PerkId.NebulaVeil);
            if (_stealthRemaining > 0f)
            {
                _stealthRemaining -= deltaTime;
            }

            if (_networkStealth.Value != active)
            {
                _networkStealth.Value = active;
            }
        }

        private void UpdateIonTrail(float deltaTime)
        {
            for (int index = 0; index < TrailCapacity; index++)
            {
                if (_trailRemaining[index] > 0f)
                {
                    _trailRemaining[index] -= deltaTime;
                }
            }

            bool emitting = _serverBoosting && HasPerk(PerkId.IonTrail) && CanActInGameplay;
            if (emitting)
            {
                _trailSpawnTimer -= deltaTime;
                if (_trailSpawnTimer <= 0f)
                {
                    _trailSpawnTimer = TrailPointInterval;
                    _trailPositions[_trailNext] = transform.position;
                    _trailRemaining[_trailNext] = PerkRules.IonTrailPointSeconds;
                    _trailNext = (_trailNext + 1) % TrailCapacity;
                }
            }

            _trailStrikeTimer -= deltaTime;
            if (_trailStrikeTimer > 0f)
            {
                return;
            }

            _trailStrikeTimer = TrailStrikeInterval;
            if (CanActInGameplay && !IsSpawnProtected)
            {
                ResolveTrailStrikes();
            }
        }

        private void ResolveTrailStrikes()
        {
            float radius = TrailRadius;
            for (int index = 0; index < TrailCapacity; index++)
            {
                if (_trailRemaining[index] <= 0f)
                {
                    continue;
                }

                Vector2 point = _trailPositions[index];
                int count = Physics2D.OverlapCircle(point, radius, _trailFilter, _trailContacts);
                for (int contact = 0; contact < count; contact++)
                {
                    Collider2D hit = _trailContacts[contact];
					if (hit == null || !hit.TryGetComponent(out PlanetBody victim) || victim == this)
                    {
                        continue;
                    }

					if (victim.TryReceiveStrike(this, point, PerkRules.IonTrailDamageScale, DamageSource.IonTrail, out float dealt))
                    {
                        Grow(dealt * MoonRules.OwnerGainFraction);
                    }
                }
            }
        }

        private void ResetAbilitiesForNewLife()
        {
            _stealthRemaining = 0f;
            _stealthCooldown = 0f;
            for (int index = 0; index < TrailCapacity; index++)
            {
                _trailRemaining[index] = 0f;
            }
        }
    }
}
