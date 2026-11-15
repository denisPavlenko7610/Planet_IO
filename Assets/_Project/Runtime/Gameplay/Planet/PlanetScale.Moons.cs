using System;
using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public abstract partial class PlanetScale
    {
        private const float MoonStepInterval = 0.04f;
        private const float MoonClashInterval = 0.12f;
        private const int MoonContactBuffer = 16;
        private const int MoonClashBuffer = 16;

        private readonly NetworkVariable<byte> _networkTier = new(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<byte> _networkMoonMask = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly MoonLoadout _moons = new();
        private readonly Collider2D[] _moonContacts = new Collider2D[MoonContactBuffer];
        private readonly Collider2D[] _clashContacts = new Collider2D[MoonClashBuffer];

        private ContactFilter2D _moonFilter;
        private CircleCollider2D _bodyCollider;
        private float _moonStepTimer;
        private float _clashStepTimer;
        private float _clashCooldown;
        private float _moonImmunityRemaining;

        public int Tier => IsSpawned ? _networkTier.Value : TierRules.Resolve(Capacity, TierRules.MinTier);

        public byte MoonMask => IsSpawned ? _networkMoonMask.Value : (byte)0;

        public int MoonSlots => SlotsForTier(Tier);

        public virtual int MoonSlotBonus => 0;

        protected virtual float MoonDamageMultiplier => 1f;

        protected virtual void OnTierIncreased(int previousTier, int newTier)
        {
        }

        private int SlotsForTier(int tier)
        {
            int baseSlots = TierRules.MoonSlots(tier);
            return baseSlots > 0 ? Mathf.Min(MoonLoadout.MaxSlots, baseSlots + MoonSlotBonus) : 0;
        }

        protected void ResetMoons()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            _moons.Reset();
            PublishMoonMask();
        }

        protected void RefreshMoonSlots()
        {
            if (IsServer && IsSpawned && _moons.SetSlots(SlotsForTier(_networkTier.Value)))
            {
                PublishMoonMask();
            }
        }

        public float MoonPhaseDegrees => (float)(NetworkObjectId % 1000UL) * 137.5f % 360f;

        public float BodyRadius
        {
            get
            {
                if (_bodyCollider == null)
                {
                    TryGetComponent(out _bodyCollider);
                }

                Vector3 scale = transform.lossyScale;
                float size = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                return _bodyCollider != null ? _bodyCollider.radius * size : 0.5f * size;
            }
        }

        public virtual bool IsHarmImmune => false;

        public virtual bool AreMoonsVisible => true;

        public event Action<int> TierChanged;

        public event Action<Vector2> MoonStruck;

        protected virtual bool IsCombatActive => true;

        protected virtual ulong AttributionClientId => Constants.UnassignedClientId;

        protected string LastAttackerName { get; private set; }

        protected ulong LastAttackerClientId { get; private set; } = Constants.UnassignedClientId;

        protected virtual void ApplyKnockback(Vector2 velocity)
        {
            ApplyKnockbackLocal(velocity);
        }

        protected void ApplyKnockbackLocal(Vector2 velocity)
        {
            if (TryGetComponent(out PlanetKnockback knockback))
            {
                knockback.Apply(velocity);
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            RefreshLeaderServer();

            float deltaTime = Time.fixedDeltaTime;
            if (_moonImmunityRemaining > 0f)
            {
                _moonImmunityRemaining -= deltaTime;
            }

            if (!IsCombatActive)
            {
                return;
            }

            if (_moons.Tick(deltaTime))
            {
                PublishMoonMask();
            }

            _moonStepTimer -= deltaTime;
            _clashStepTimer -= deltaTime;
            if (_clashCooldown > 0f)
            {
                _clashCooldown -= deltaTime;
            }

            if (_moons.AliveMask == 0 || IsHarmImmune)
            {
                return;
            }

            if (_moonStepTimer <= 0f)
            {
                _moonStepTimer = MoonStepInterval;
                ResolveMoonContacts();
            }

            if (_clashStepTimer <= 0f && _clashCooldown <= 0f)
            {
                _clashStepTimer = MoonClashInterval;
                ResolveMoonClashes();
            }
        }

        public bool IsMoonThreat(Vector2 point, float radius, out Vector2 moonPosition)
        {
            moonPosition = default;
            if (!IsServer || !IsSpawned || !IsCombatActive || IsHarmImmune || _moons.AliveMask == 0)
            {
                return false;
            }

            Vector2 center = transform.position;
            float bodyRadius = BodyRadius;
            float threatRadius = radius * 1.5f + MoonRules.MoonRadius(bodyRadius) + MoonRules.BotThreatMargin;
            float bestDistanceSquared = threatRadius * threatRadius;
            double time = OrbitClock.Now;
            float phase = MoonPhaseDegrees;
            int slots = _moons.Slots;
            bool found = false;

            for (int index = 0; index < slots; index++)
            {
                if (!_moons.IsAlive(index))
                {
                    continue;
                }

                Vector2 position = MoonRules.Position(center, bodyRadius, index, slots, time, phase);
                float distanceSquared = (position - point).sqrMagnitude;
                if (distanceSquared <= bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    moonPosition = position;
                    found = true;
                }
            }

            return found;
        }

        private void InitializeMoons()
        {
            _moonFilter = ContactFilter2D.noFilter;
            _moonFilter.SetLayerMask(~LayerMask.GetMask(Constants.FoodLayerName));
        }

        private void SubscribeMoons()
        {
            _networkTier.OnValueChanged += OnTierValueChanged;
        }

        private void UnsubscribeMoons()
        {
            _networkTier.OnValueChanged -= OnTierValueChanged;
            _moons.Reset();
        }

        private void AttachMoonViews()
        {
            if (!TryGetComponent(out MoonRingView _))
            {
                gameObject.AddComponent<MoonRingView>();
            }

            if (!TryGetComponent(out ThreatRingView _))
            {
                gameObject.AddComponent<ThreatRingView>();
            }

            if (!TryGetComponent(out PlanetKnockback _))
            {
                gameObject.AddComponent<PlanetKnockback>();
            }

            if (!TryGetComponent(out PlanetGravity _))
            {
                gameObject.AddComponent<PlanetGravity>();
            }

            if (!TryGetComponent(out LeaderBeaconView _))
            {
                gameObject.AddComponent<LeaderBeaconView>();
            }
        }

        private void RefreshTierServer(float capacity)
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            int previous = _networkTier.Value;
            int tier = TierRules.Resolve(capacity, previous);
            if (tier != previous)
            {
                _networkTier.Value = (byte)tier;
            }

            if (_moons.SetSlots(SlotsForTier(tier)))
            {
                PublishMoonMask();
            }

            if (tier > previous)
            {
                OnTierIncreased(previous, tier);
            }
        }

        private void PublishMoonMask()
        {
            if (_networkMoonMask.Value != _moons.AliveMask)
            {
                _networkMoonMask.Value = _moons.AliveMask;
            }
        }

        private void OnTierValueChanged(byte previous, byte current)
        {
            TierChanged?.Invoke(current);

            if (current > previous && previous >= TierRules.MinTier)
            {
                PlayTierUpEffect();
            }
        }

        private void PlayTierUpEffect()
        {
            Color color = TryGetComponent(out SpriteRenderer body) ? body.color : Color.white;
            Vector2 position = transform.position;
            float size = transform.localScale.x;
            GameVfx.Eat(position, color, size);
            GameVfx.Impact(position, size);
        }

        private void ResolveMoonContacts()
        {
            Vector2 center = transform.position;
            float bodyRadius = BodyRadius;
            float moonRadius = MoonRules.MoonRadius(bodyRadius);
            double time = OrbitClock.Now;
            float phase = MoonPhaseDegrees;
            int slots = _moons.Slots;

            for (int index = 0; index < slots; index++)
            {
                if (!_moons.IsAlive(index))
                {
                    continue;
                }

                Vector2 position = MoonRules.Position(center, bodyRadius, index, slots, time, phase);
                int count = Physics2D.OverlapCircle(position, moonRadius, _moonFilter, _moonContacts);

                for (int contact = 0; contact < count; contact++)
                {
                    Collider2D hit = _moonContacts[contact];
                    if (hit == null)
                    {
                        continue;
                    }

                    if (hit.TryGetComponent(out PlanetScale victim))
                    {
                        if (victim != this)
                        {
                            StrikeWithMoon(victim, index, position, moonRadius);
                        }
                    }
                    else if (hit.TryGetComponent(out Comet comet))
                    {
                        InterceptComet(comet, index);
                    }

                    if (!_moons.IsAlive(index))
                    {
                        break;
                    }
                }
            }
        }

        private void StrikeWithMoon(PlanetScale victim, int moonIndex, Vector2 moonPosition, float moonRadius)
        {
            if (!victim.TryReceiveStrike(this, moonPosition, MoonDamageMultiplier, out float dealt))
            {
                return;
            }

            Grow(dealt * MoonRules.OwnerGainFraction);
            PlayMoonImpactRpc(moonPosition, moonRadius);

            if (_moons.Hit(moonIndex))
            {
                PublishMoonMask();
            }
        }

        private void InterceptComet(Comet comet, int moonIndex)
        {
            comet.PlayImpactEffect();
            CometRespawnService?.Respawn(comet);

            if (_moons.Hit(moonIndex))
            {
                PublishMoonMask();
            }
        }

        internal bool TryReceiveStrike(PlanetScale attacker, Vector2 sourcePosition, float damageScale, out float dealt)
        {
            dealt = 0f;
            if (!IsServer ||
                !IsSpawned ||
                !IsCombatActive ||
                IsHarmImmune ||
                _moonImmunityRemaining > 0f)
            {
                return false;
            }

            float before = Capacity;
            _moonImmunityRemaining = MoonRules.HitImmunitySeconds;

            LastAttackerName = attacker.DisplayName;
            LastAttackerClientId = attacker.AttributionClientId;
            Shrink(MoonRules.Damage(before) * damageScale);
            LastAttackerName = null;
            LastAttackerClientId = Constants.UnassignedClientId;

            dealt = Mathf.Max(0f, before - Capacity);

            Vector2 away = (Vector2)transform.position - sourcePosition;
            Vector2 direction = away.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude
                ? away.normalized
                : Constants.GetRandomDirection();
            ApplyKnockback(direction * MoonRules.KnockbackSpeed);
            return true;
        }

        private void ResolveMoonClashes()
        {
            Vector2 center = transform.position;
            float searchRadius = MoonRules.OuterExtent(BodyRadius) + MoonRules.ClashSearchPadding;
            int count = Physics2D.OverlapCircle(center, searchRadius, _moonFilter, _clashContacts);
            double time = OrbitClock.Now;

            for (int contact = 0; contact < count; contact++)
            {
                Collider2D hit = _clashContacts[contact];
                if (hit == null ||
                    !hit.TryGetComponent(out PlanetScale other) ||
                    other == this ||
                    !other.CanClashMoons)
                {
                    continue;
                }

                if (TryFindMoonClash(other, time, out Vector2 clashPoint))
                {
                    StartMoonClash(other, clashPoint);
                    return;
                }
            }
        }

        private bool CanClashMoons =>
            IsServer && IsSpawned && IsCombatActive && !IsHarmImmune && _clashCooldown <= 0f && _moons.AliveMask != 0;

        private bool TryFindMoonClash(PlanetScale other, double time, out Vector2 clashPoint)
        {
            clashPoint = default;
            Vector2 centerA = transform.position;
            Vector2 centerB = other.transform.position;
            float radiusA = BodyRadius;
            float radiusB = other.BodyRadius;
            float reach = MoonRules.OuterExtent(radiusA) + MoonRules.OuterExtent(radiusB);
            if ((centerA - centerB).sqrMagnitude > reach * reach)
            {
                return false;
            }

            float limit = MoonRules.MoonRadius(radiusA) + MoonRules.MoonRadius(radiusB);
            float limitSquared = limit * limit;
            float phaseA = MoonPhaseDegrees;
            float phaseB = other.MoonPhaseDegrees;

            for (int a = 0; a < _moons.Slots; a++)
            {
                if (!_moons.IsAlive(a))
                {
                    continue;
                }

                Vector2 positionA = MoonRules.Position(centerA, radiusA, a, _moons.Slots, time, phaseA);

                for (int b = 0; b < other._moons.Slots; b++)
                {
                    if (!other._moons.IsAlive(b))
                    {
                        continue;
                    }

                    Vector2 positionB = MoonRules.Position(centerB, radiusB, b, other._moons.Slots, time, phaseB);
                    if ((positionA - positionB).sqrMagnitude <= limitSquared)
                    {
                        clashPoint = (positionA + positionB) * 0.5f;
                        return true;
                    }
                }
            }

            return false;
        }

        private void StartMoonClash(PlanetScale other, Vector2 clashPoint)
        {
            _clashCooldown = MoonRules.ClashCooldownSeconds;
            other._clashCooldown = MoonRules.ClashCooldownSeconds;

            Vector2 apart = (Vector2)transform.position - (Vector2)other.transform.position;
            Vector2 direction = apart.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude
                ? apart.normalized
                : Constants.GetRandomDirection();

            ApplyKnockback(direction * MoonRules.ClashKnockbackSpeed);
            other.ApplyKnockback(-direction * MoonRules.ClashKnockbackSpeed);
            PlayMoonImpactRpc(clashPoint, MoonRules.MoonRadius(BodyRadius));
        }

        [Rpc(SendTo.Everyone)]
        private void PlayMoonImpactRpc(Vector2 position, float size)
        {
            GameVfx.Impact(position, size);
            MoonStruck?.Invoke(position);
        }
    }
}
