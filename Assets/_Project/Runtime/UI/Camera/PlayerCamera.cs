using System;
using UnityEngine;
using PlanetIO.UI.Hud;
using VContainer;

namespace PlanetIO.UI.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DefaultExecutionOrder(1000)]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.08f;
        [SerializeField, Min(0.01f)] private float _zoomSmoothTime = 0.2f;
        [SerializeField, Min(0.1f)] private float _baseOrthographicSize = 3.6f;
        [SerializeField, Range(0f, 1f)] private float _zoomExponent = 0.28f;
        [SerializeField, Min(1f)] private float _maximumOrthographicSize = 30f;
        [SerializeField, Min(0.1f)] private float _minimumOrthographicSize = 2f;
        [SerializeField, Min(1f)] private float _moonViewMargin = 1.45f;
        [SerializeField, Range(0f, 0.5f)] private float _lookAheadFactor = 0.16f;
        [SerializeField, Min(0.01f)] private float _lookAheadSmoothTime = 0.35f;
        [SerializeField, Min(1f)] private float _boostZoom = 1.08f;
        [SerializeField, Min(0f)] private float _killShake = 0.25f;
        [SerializeField, Min(0f)] private float _deathShake = 0.6f;
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.35f;

        private Player _player;
        private ILocalPlayerProvider _localPlayerProvider;
        private Vector3 _positionVelocity;
        private Vector2 _lookAhead;
        private Vector2 _lookAheadVelocity;
        private float _zoomVelocity;
        private float _cameraDepth;
        private float _targetOrthographicSize;
        private float _shakeStrength;
        private float _shakeTimeRemaining;
        private Vector3 _lastShakeOffset;

        public UnityEngine.Camera Camera { get; private set; }

        private void Awake()
        {
            Camera = GetComponent<UnityEngine.Camera>();
            _cameraDepth = transform.position.z;
            _targetOrthographicSize = _baseOrthographicSize;
            Camera.orthographicSize = _baseOrthographicSize;
            FoodViewScale.SetZoomRatio(1f);
        }

        [Inject]
        public void Construct(ILocalPlayerProvider localPlayerProvider)
        {
            _localPlayerProvider = localPlayerProvider ??
                throw new ArgumentNullException(nameof(localPlayerProvider));
            _localPlayerProvider.LocalPlayerChanged += OnLocalPlayerChanged;
            OnLocalPlayerChanged(_localPlayerProvider.LocalPlayer);
        }

        private void LateUpdate()
        {
            if (_player == null || !_player.IsSpawned)
            {
                return;
            }

            Vector3 playerPosition = _player.transform.position;
            UpdateLookAhead();
            Vector3 targetPosition = new(
                playerPosition.x + _lookAhead.x,
                playerPosition.y + _lookAhead.y,
                _cameraDepth);
            Vector3 basePosition = transform.position - _lastShakeOffset;
            Vector3 smoothed = Vector3.SmoothDamp(basePosition, targetPosition, ref _positionVelocity, _positionSmoothTime);
            _lastShakeOffset = GetShakeOffset();
            transform.position = smoothed + _lastShakeOffset;

            float zoomTarget = _player.IsBoosting ? _targetOrthographicSize * _boostZoom : _targetOrthographicSize;
            Camera.orthographicSize = Mathf.SmoothDamp(Camera.orthographicSize, zoomTarget, ref _zoomVelocity,
                _zoomSmoothTime);
        }

        private void OnDestroy()
        {
            if (_localPlayerProvider != null)
            {
                _localPlayerProvider.LocalPlayerChanged -= OnLocalPlayerChanged;
            }

            UnbindPlayer();
            FoodViewScale.SetZoomRatio(1f);
        }

        private void UpdateLookAhead()
        {
            Vector2 desired = _player.IsDefeated
                ? Vector2.zero
                : (Vector2)_player.transform.right * (Camera.orthographicSize * _lookAheadFactor);
            _lookAhead = Vector2.SmoothDamp(_lookAhead, desired, ref _lookAheadVelocity, _lookAheadSmoothTime);
        }

        private void OnLocalPlayerChanged(Player player)
        {
            UnbindPlayer();
            _player = player;
            _positionVelocity = Vector3.zero;
            _lookAhead = Vector2.zero;
            _lookAheadVelocity = Vector2.zero;

            if (_player == null)
            {
                return;
            }

            _player.CapacityChanged += OnCapacityChanged;
            _player.TierChanged += OnTierChanged;
            _player.Defeated += OnPlayerDefeated;
            _player.Killed += OnPlayerKilled;
            OnCapacityChanged(_player.Capacity);
        }

        private void UnbindPlayer()
        {
            if (_player == null)
            {
                return;
            }

            _player.CapacityChanged -= OnCapacityChanged;
            _player.TierChanged -= OnTierChanged;
            _player.Defeated -= OnPlayerDefeated;
            _player.Killed -= OnPlayerKilled;
            _player = null;
        }

        private void OnTierChanged(int _)
        {
            if (_player != null)
            {
                OnCapacityChanged(_player.Capacity);
            }
        }

        private void OnPlayerDefeated() => Shake(_deathShake);

        private void OnPlayerKilled(string _, int __) => Shake(_killShake);

        private void Shake(float strength)
        {
            _shakeStrength = Mathf.Max(_shakeStrength, strength);
            _shakeTimeRemaining = _shakeDuration;
        }

        private Vector3 GetShakeOffset()
        {
            if (_shakeTimeRemaining <= 0f)
            {
                return Vector3.zero;
            }

            _shakeTimeRemaining -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(_shakeTimeRemaining / _shakeDuration);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * (_shakeStrength * fade);
            if (_shakeTimeRemaining <= 0f)
            {
                _shakeStrength = 0f;
            }

            return new Vector3(offset.x, offset.y, 0f);
        }

        private void OnCapacityChanged(float capacity)
        {
            float target = _baseOrthographicSize * CameraZoomRules.Ratio(capacity, _zoomExponent);

            if (_player != null && _player.MoonSlots > 0)
            {
                float moonRingSize = MoonRules.OuterExtent(_player.BodyRadius) * _moonViewMargin;
                target = Mathf.Max(target, moonRingSize);
            }

            _targetOrthographicSize = Mathf.Clamp(target, _minimumOrthographicSize, _maximumOrthographicSize);
            FoodViewScale.SetZoomRatio(_targetOrthographicSize / _baseOrthographicSize);
        }
    }
}
