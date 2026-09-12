using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed class Food : NetworkBehaviour, ICapacity
    {
        private static readonly Color GoldenTint = new(1f, 0.82f, 0.25f);

        private readonly NetworkVariable<float> _valueMultiplier = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private SpriteRenderer _spriteRenderer;

        public float Capacity { get; set; }
        public bool IsDropped { get; private set; }

        public int LifecycleVersion { get; private set; }

        public float ValueMultiplier => _valueMultiplier.Value;

        private bool _isClaimed;

        public bool TryClaim()
        {
            if (_isClaimed)
            {
                return false;
            }

            _isClaimed = true;
            return true;
        }

        public void ResetClaim()
        {
            _isClaimed = false;
        }

        public int MarkAsDropped()
        {
            IsDropped = true;
            return ++LifecycleVersion;
        }

        public void MarkAsStored()
        {
            IsDropped = false;
            LifecycleVersion++;
        }

        public void SetValueMultiplier(float value)
        {
            if (IsServer)
            {
                _valueMultiplier.Value = Mathf.Max(1f, value);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _valueMultiplier.OnValueChanged += OnValueMultiplierChanged;
            ApplyGoldenTint(_valueMultiplier.Value);
        }

        public override void OnNetworkDespawn()
        {
            _valueMultiplier.OnValueChanged -= OnValueMultiplierChanged;
            base.OnNetworkDespawn();
        }

        private void OnValueMultiplierChanged(float _, float value)
        {
            ApplyGoldenTint(value);
        }

        private void ApplyGoldenTint(float value)
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = value > 1f ? GoldenTint : Color.white;
            }
        }
    }
}
