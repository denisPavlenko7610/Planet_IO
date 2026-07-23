using System;
using Unity.Netcode;
using UnityEngine;

namespace Planet_IO
{
    public abstract class PlanetScale : NetworkBehaviour, ICapacity
    {
        [field: Header("Capacity")]
        [field: SerializeField, Min(0.01f)]
        public float MinCapacity { get; private set; } = 0.08f;

        [SerializeField, Min(0.02f)] private float _maxCapacity = 2f;

        private readonly NetworkVariable<float> _networkCapacity = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private float _localCapacity;

        public float Capacity
        {
            get => IsSpawned ? _networkCapacity.Value : _localCapacity;
            set => SetCapacityAbsolute(value);
        }

        public event Action<float> CapacityChanged;

        protected virtual void Awake()
        {
            _localCapacity = ClampCapacity(transform.localScale.x);
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
            return Mathf.Clamp(value, MinCapacity, Mathf.Max(MinCapacity, _maxCapacity));
        }
    }
}
