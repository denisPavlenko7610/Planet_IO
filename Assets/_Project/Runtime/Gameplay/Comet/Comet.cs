using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed class Comet : NetworkBehaviour, ICapacity
    {
        private const float WarningDistance = 9f;
        private const float WarningPulseSpeed = 10f;
        private static readonly Color WarningColor = new(1f, 0.35f, 0.3f);

        private SpriteRenderer _spriteRenderer;
        private Color _baseColor = Color.white;

        public float Capacity { get; set; }

        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
            }
        }

        private void Update()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            float danger = 0f;
            Player localPlayer = PlayerRegistry.LocalPlayer;
            if (localPlayer != null && !localPlayer.IsDefeated)
            {
                float distance = Vector2.Distance(localPlayer.transform.position, transform.position);
                danger = 1f - Mathf.Clamp01(distance / WarningDistance);
            }

            float pulse = danger > 0f ? 0.5f + 0.5f * Mathf.Sin(Time.time * WarningPulseSpeed) : 0f;
            _spriteRenderer.color = Color.Lerp(_baseColor, WarningColor, danger * pulse);
        }

        public void PlayImpactEffect()
        {
            if (IsServer && IsSpawned)
            {
                PlayImpactEffectRpc(transform.position, transform.localScale.x);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PlayImpactEffectRpc(Vector2 position, float size)
        {
            GameVfx.Impact(position, size);
        }
    }
}
