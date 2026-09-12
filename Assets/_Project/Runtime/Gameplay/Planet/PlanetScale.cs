using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlanetIO
{
    public abstract class PlanetScale : NetworkBehaviour, ICapacity
    {
        private const float MinAllowedCapacity = 0.01f;
        private const float ScaleDepth = 1f;
        private const int DisplayNameMaximumLength = 32;

        [Header("Capacity")]
        [FormerlySerializedAs("_minimumCapacity")]
        [SerializeField, Min(MinAllowedCapacity)] private float _minCapacity = 0.08f;
        [FormerlySerializedAs("_maximumCapacity")]
        [SerializeField, Min(MinAllowedCapacity)] private float _maxCapacity = 1f;

        private readonly NetworkVariable<float> _networkCapacity = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<FixedString64Bytes> _networkDisplayName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float _localCapacity;

        public float MinCapacity => _minCapacity;

        public float Capacity
        {
            get => IsSpawned ? _networkCapacity.Value : _localCapacity;
            set => TrySetCapacity(value);
        }

        public string DisplayName =>
            _networkDisplayName.Value.Length > 0
                ? _networkDisplayName.Value.ToString()
                : GetFallbackDisplayName();

        public event Action<float> CapacityChanged;
        public event Action<string> DisplayNameChanged;

        protected virtual void Awake()
        {
            NormalizeCapacityBounds();
            ApplyCapacity(_minCapacity, false);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _networkCapacity.Value = ClampCapacity(_localCapacity);
            }

            _networkCapacity.OnValueChanged += OnNetworkCapacityChanged;
            _networkDisplayName.OnValueChanged += OnDisplayNameValueChanged;
            ApplyCapacity(_networkCapacity.Value, true);
        }

        public override void OnNetworkDespawn()
        {
            float lastCapacity = _networkCapacity.Value;
            _networkCapacity.OnValueChanged -= OnNetworkCapacityChanged;
            _networkDisplayName.OnValueChanged -= OnDisplayNameValueChanged;
            _localCapacity = ClampCapacity(lastCapacity);
            base.OnNetworkDespawn();
        }

        protected abstract float FoodGrowthMultiplier { get; }
        protected abstract float CometDamageMultiplier { get; }
        protected abstract string GetFallbackDisplayName();
        protected IRespawnService<Food> FoodRespawnService { get; set; }
        protected IRespawnService<Comet> CometRespawnService { get; set; }

        protected void SetDisplayName(string value)
        {
            if (!IsServer)
            {
                return;
            }

            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length > DisplayNameMaximumLength)
            {
                normalized = normalized[..DisplayNameMaximumLength];
            }

            _networkDisplayName.Value = normalized;
        }

        protected bool Grow(float amount)
        {
            return ChangeCapacity(Mathf.Abs(amount));
        }

        protected bool Shrink(float amount)
        {
            return ChangeCapacity(-Mathf.Abs(amount));
        }

        protected virtual void DeathCheck(float capacity)
        {
        }

        protected void HandleEntityCollision(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.TryGetComponent(out Food point))
            {
                if (!point.TryClaim())
                {
                    return;
                }

                Grow(point.Capacity * FoodGrowthMultiplier * point.ValueMultiplier);
                FoodRespawnService?.Respawn(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                Shrink(comet.Capacity * CometDamageMultiplier);
                CometRespawnService?.Respawn(comet);
            }
        }

        private bool ChangeCapacity(float delta)
        {
            return TrySetCapacity(Capacity + delta);
        }

        private bool TrySetCapacity(float value)
        {
            if (float.IsNaN(value) || IsSpawned && !IsServer)
            {
                return false;
            }

            float clamped = ClampCapacity(value);
            if (Capacity == clamped)
            {
                return false;
            }

            if (IsSpawned)
            {
                _networkCapacity.Value = clamped;
            }
            else
            {
                ApplyCapacity(clamped, true);
            }

            return true;
        }

        private void OnNetworkCapacityChanged(float _, float current)
        {
            ApplyCapacity(current, true);
        }

        private void OnDisplayNameValueChanged(FixedString64Bytes _, FixedString64Bytes value)
        {
            DisplayNameChanged?.Invoke(value.ToString());
        }

        private void ApplyCapacity(float capacity, bool notify)
        {
            _localCapacity = capacity;
            transform.localScale = new Vector3(capacity, capacity, ScaleDepth);

            if (notify)
            {
                CapacityChanged?.Invoke(capacity);
                DeathCheck(capacity);
            }
        }

        private float ClampCapacity(float value)
        {
            return Mathf.Clamp(value, _minCapacity, _maxCapacity);
        }

        private void NormalizeCapacityBounds()
        {
            _minCapacity = IsFinite(_minCapacity)
                ? Mathf.Max(MinAllowedCapacity, _minCapacity)
                : MinAllowedCapacity;
            _maxCapacity = IsFinite(_maxCapacity)
                ? Mathf.Max(_minCapacity, _maxCapacity)
                : _minCapacity;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeCapacityBounds();
        }
#endif
    }
}
