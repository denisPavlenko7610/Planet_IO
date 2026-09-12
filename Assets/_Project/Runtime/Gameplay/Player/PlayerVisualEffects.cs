using UnityEngine;

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
        [SerializeField, Range(0f, 1f)] private float _boostVolume = 0.3f;

        private SpriteRenderer _spriteRenderer;
        private Color _baseColor;
        private AudioSource _boostSource;
        private bool _boosting;
        private bool _protected;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;

            _boostSource = gameObject.AddComponent<AudioSource>();
            _boostSource.loop = true;
            _boostSource.playOnAwake = false;
            _boostSource.volume = _boostVolume;
            _boostSource.clip = GameAudio.Load("boost_loop");
        }

        public void SetBaseColor(Color color)
        {
            _baseColor = color;
            if (!_boosting && !_protected)
            {
                RestoreColor();
            }
        }

        public void SetBoosting(bool boosting)
        {
            if (_boosting == boosting)
            {
                return;
            }

            _boosting = boosting;
            if (_boostSource != null && _boostSource.clip != null)
            {
                if (boosting)
                {
                    _boostSource.Play();
                }
                else
                {
                    _boostSource.Stop();
                }
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
            if (_boostSource != null && _boostSource.isPlaying)
            {
                _boostSource.Stop();
            }
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
