using PlanetIO.Utils;
using UnityEngine;
using PlanetIO.UI.Hud;
using VContainer;

namespace PlanetIO.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DefaultExecutionOrder(1000)]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _positionSmoothTime = 0.08f;
        [SerializeField, Min(0.01f)] private float _zoomSmoothTime = 0.2f;
        [SerializeField, Min(0f)] private float _zoomPerCapacityUnit = 5f;
        [SerializeField, Min(1f)] private float _maximumOrthographicSize = 30f;

        private Player _player;
        private ILocalPlayerProvider _localPlayerProvider;
        private Vector3 _positionVelocity;
        private float _zoomVelocity;
        private float _cameraDepth;
        private float _baseOrthographicSize;
        private float _baseCapacity;
        private float _targetOrthographicSize;

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
            _localPlayerProvider = localPlayerProvider;
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
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _positionVelocity,
                _positionSmoothTime);

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
            OnCapacityChanged(_player.Capacity);
        }

        private void UnbindPlayer()
        {
            if (_player == null)
            {
                return;
            }

            _player.CapacityChanged -= OnCapacityChanged;
            _player = null;
        }

        private void OnCapacityChanged(float capacity)
        {
            float additionalZoom = Mathf.Max(0f, capacity - _baseCapacity) * _zoomPerCapacityUnit;
            _targetOrthographicSize = Mathf.Clamp(_baseOrthographicSize + additionalZoom, _baseOrthographicSize,
                _maximumOrthographicSize);
        }
    }
}
