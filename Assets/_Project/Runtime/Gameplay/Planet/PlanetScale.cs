using System;
using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public abstract class PlanetScale : NetworkBehaviour, ICapacity
    {
        [Header("Capacity")]
        [SerializeField, Min(0.01f)] private float _minimumCapacity = 0.08f;
        [SerializeField, Min(0.02f)] private float _maximumCapacity = 1f;

        private readonly NetworkVariable<float> _networkCapacity = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float _localCapacity;

        public float MinimumCapacity => _minimumCapacity;

        public float Capacity
        {
            get => IsSpawned ? _networkCapacity.Value : _localCapacity;
            set => SetCapacityAbsolute(value);
        }

        public event Action<float> CapacityChanged;

        protected virtual void Awake()
        {
            _localCapacity = _minimumCapacity;
            ApplyCapacity(_localCapacity, false);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkCapacity.OnValueChanged += OnNetworkCapacityChanged;

            if (IsServer)
            {
                _networkCapacity.Value = ClampCapacity(_localCapacity);
            }

            ApplyCapacity(_networkCapacity.Value, true);
        }

        public override void OnNetworkDespawn()
        {
            _networkCapacity.OnValueChanged -= OnNetworkCapacityChanged;
            _localCapacity = Capacity;
            base.OnNetworkDespawn();
        }

        protected float FoodGrowthMultiplier { get; set; } = 0.01f;
        protected float CometDamageMultiplier { get; set; } = 0.02f;
        protected IRespawnService<Point> PointRespawnService { get; set; }
        protected IRespawnService<Comet> CometRespawnService { get; set; }

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
            if (other.TryGetComponent(out Point point))
            {
                Grow(point.Capacity * FoodGrowthMultiplier);
                PointRespawnService?.Respawn(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                Shrink(comet.Capacity * CometDamageMultiplier);
                CometRespawnService?.Respawn(comet);
            }
        }

        private bool ChangeCapacity(float delta)
        {
            if (IsSpawned && !IsServer)
            {
                return false;
            }

            float before = Capacity;
            SetCapacityAbsolute(before + delta);
            return !Mathf.Approximately(before, Capacity);
        }

        private void SetCapacityAbsolute(float value)
        {
            float clamped = ClampCapacity(value);

            if (IsSpawned)
            {
                if (!IsServer)
                {
                    return;
                }

                if (Mathf.Approximately(_networkCapacity.Value, clamped))
                {
                    return;
                }

                _networkCapacity.Value = clamped;
                return;
            }

            _localCapacity = clamped;
            ApplyCapacity(clamped, true);
        }

        private void OnNetworkCapacityChanged(float previous, float current)
        {
            ApplyCapacity(current, true);
        }

        private void ApplyCapacity(float capacity, bool notify)
        {
            _localCapacity = capacity;
            transform.localScale = new Vector3(capacity, capacity, 1f);

            if (notify)
            {
                CapacityChanged?.Invoke(capacity);
                DeathCheck(capacity);
            }
        }

        private float ClampCapacity(float value)
        {
            return Mathf.Clamp(
                value,
                _minimumCapacity,
                Mathf.Max(_minimumCapacity, _maximumCapacity));
        }
    }
}
