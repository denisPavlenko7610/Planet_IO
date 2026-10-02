using System;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerVisualEffects : MonoBehaviour
    {
        private const float BoostTintStrength = 0.65f;
        private static readonly Color BoostTint = new(1f, 0.85f, 0.4f);
        private static readonly Color ProtectionTint = new(1f, 1f, 1f);

        [SerializeField, Min(0.1f)] private float _boostPulseSpeed = 8f;
        [SerializeField, Min(0.1f)] private float _protectionBlinkSpeed = 14f;
        [SerializeField, Range(0f, 1f)] private float _protectionBlinkAmount = 0.7f;
        [SerializeField] private TrailRenderer _boostTrail;
        [SerializeField, Min(0f)] private float _trailWidthPerScale = 0.8f;

        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;
        private ISfxPlayer _sfxPlayer;
        private IDisposable _boostLoop;
        private bool _localAudio;
        private bool _boosting;
        private bool _protected;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        }

        [Inject]
        public void Construct(ISfxPlayer sfxPlayer)
        {
            _sfxPlayer = sfxPlayer;
        }

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            ApplyTrailColor(color);

            if (_spriteRenderer != null && !_boosting && !_protected)
            {
                _spriteRenderer.color = color;
            }
        }

        public void SetLocalAudio(bool local)
        {
            _localAudio = local;
            if (!local)
            {
                StopBoostLoop();
            }
        }

        public void SetBoosting(bool boosting)
        {
            if (_boosting == boosting)
            {
                return;
            }

            _boosting = boosting;
            if (_boostTrail != null)
            {
                _boostTrail.emitting = boosting;
            }

            if (boosting && _localAudio && _sfxPlayer != null)
            {
                _boostLoop ??= _sfxPlayer.PlayLoop(SfxId.Boost, transform);
            }
            else
            {
                StopBoostLoop();
            }

            if (!_boosting && !_protected)
            {
                RestoreColor();
            }
        }

        public void SetSpawnProtected(bool protection)
        {
            if (_protected == protection)
            {
                return;
            }

            _protected = protection;
            if (!_protected && !_boosting)
            {
                RestoreColor();
            }
        }

        private void Update()
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            if (_boostTrail != null && _boostTrail.emitting)
            {
                _boostTrail.widthMultiplier = transform.lossyScale.x * _trailWidthPerScale;
            }

            if (_protected)
            {
                float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * _protectionBlinkSpeed);
                _spriteRenderer.color = Color.Lerp(_baseColor, ProtectionTint, blink * _protectionBlinkAmount);
            }
            else if (_boosting)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * _boostPulseSpeed);
                _spriteRenderer.color = Color.Lerp(_baseColor, BoostTint, pulse * BoostTintStrength);
            }
        }

        private void OnDisable()
        {
            _boosting = false;
            _protected = false;
            RestoreColor();
            StopBoostLoop();
        }

        private void ApplyTrailColor(Color color)
        {
            if (_boostTrail == null)
            {
                return;
            }

            Color start = color;
            start.a = 0.85f;
            Color end = color;
            end.a = 0f;
            _boostTrail.startColor = start;
            _boostTrail.endColor = end;
        }

        private void StopBoostLoop()
        {
            _boostLoop?.Dispose();
            _boostLoop = null;
        }

        private void RestoreColor()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }
        }
    }
}
