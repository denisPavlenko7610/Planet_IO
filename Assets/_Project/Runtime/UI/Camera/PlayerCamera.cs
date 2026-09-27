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
        [SerializeField, Min(0f)] private float _zoomPerCapacityUnit = 5f;
        [SerializeField, Min(1f)] private float _maximumOrthographicSize = 30f;
        [SerializeField, Min(0.1f)] private float _minimumOrthographicSize = 2f;
        [SerializeField, Min(0f)] private float _killShake = 0.25f;
        [SerializeField, Min(0f)] private float _deathShake = 0.6f;
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.35f;

        private Player _player;
        private ILocalPlayerProvider _localPlayerProvider;
        private Vector3 _positionVelocity;
        private float _zoomVelocity;
        private float _cameraDepth;
        private float _baseOrthographicSize;
        private float _baseCapacity;
        private float _targetOrthographicSize;
        private float _shakeStrength;
        private float _shakeTimeRemaining;
        private Vector3 _lastShakeOffset;

        public UnityEngine.Camera Camera { get; private set; }

        private void Awake()
        {
            Camera = GetComponent<UnityEngine.Camera>();
            _cameraDepth = transform.position.z;
            _baseOrthographicSize = Camera.orthographicSize;
            _targetOrthographicSize = _baseOrthographicSize;
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
            Vector3 targetPosition = new(playerPosition.x, playerPosition.y, _cameraDepth);
            Vector3 basePosition = transform.position - _lastShakeOffset;
            Vector3 smoothed = Vector3.SmoothDamp(basePosition, targetPosition, ref _positionVelocity, _positionSmoothTime);
            _lastShakeOffset = GetShakeOffset();
            transform.position = smoothed + _lastShakeOffset;

            Camera.orthographicSize = Mathf.SmoothDamp(Camera.orthographicSize, _targetOrthographicSize, ref _zoomVelocity,
                _zoomSmoothTime);
        }

        private void OnDestroy()
        {
            if (_localPlayerProvider != null)
            {
                _localPlayerProvider.LocalPlayerChanged -= OnLocalPlayerChanged;
            }

            UnbindPlayer();
        }

        private void OnLocalPlayerChanged(Player player)
        {
            UnbindPlayer();
            _player = player;
            _positionVelocity = Vector3.zero;

            if (_player == null)
            {
                return;
            }

            _baseCapacity = Mathf.Max(_player.Capacity, Constants.MinimumDisplayCapacity);
            _player.CapacityChanged += OnCapacityChanged;
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
            _player.Defeated -= OnPlayerDefeated;
            _player.Killed -= OnPlayerKilled;
            _player = null;
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
            float additionalZoom = (capacity - _baseCapacity) * _zoomPerCapacityUnit;
            _targetOrthographicSize = Mathf.Clamp(_baseOrthographicSize + additionalZoom,
                _minimumOrthographicSize, _maximumOrthographicSize);
        }
    }
}
