using PlanetIO.Core.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace Planet_IO.Arrow
{
    public sealed class Arrow : MonoBehaviour
    {
        [SerializeField, Assign] private Image _arrowImage;
        [SerializeField, Min(0f)] private float _distanceFromPlayer = 0.65f;
        [SerializeField, Min(0.1f)] private float _playerVisualRadius = 3.55f;
        [SerializeField] private float _spriteAngleOffset = 90f;
        [SerializeField, Range(0.5f, 2f)] private float _maximumScale = 1.4f;

        private Player _player;
        private PlayerMovement _playerMovement;
        private UnityEngine.Camera _camera;
        private float _initialCapacity;

        private void Awake()
        {
            _camera = UnityEngine.Camera.main;
            _arrowImage.raycastTarget = false;
            _arrowImage.enabled = false;
        }

        private void LateUpdate()
        {
            if (!TryBindLocalPlayer())
            {
                _arrowImage.enabled = false;
                return;
            }

            Vector2 direction = _playerMovement.Direction.normalized;
            if (direction == Vector2.zero)
            {
                direction = _player.transform.right;
            }

            float playerRadius = _player.Capacity * _playerVisualRadius;
            Vector3 worldPosition = _player.transform.position +
                                (Vector3)(direction * (playerRadius + _distanceFromPlayer));

            _arrowImage.enabled = true;
            _arrowImage.rectTransform.position = _camera.WorldToScreenPoint(worldPosition);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _arrowImage.rectTransform.rotation =
                Quaternion.Euler(0f, 0f, angle + _spriteAngleOffset);

            float screenScale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f);
            float massScale = Mathf.Clamp(
                Mathf.Sqrt(_player.Capacity / _initialCapacity),
                0.8f,
                _maximumScale);
            _arrowImage.rectTransform.localScale =
                Vector3.one * screenScale * massScale;
        }

        private bool TryBindLocalPlayer()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }

            if (_camera == null)
            {
                return false;
            }

            if (_playerMovement != null &&
                _player != null &&
                _playerMovement.IsSpawned &&
                _playerMovement.IsOwner)
            {
                return true;
            }

            foreach (PlayerMovement candidate in FindObjectsByType<PlayerMovement>(
                         FindObjectsInactive.Exclude))
            {
                if (!candidate.IsSpawned || !candidate.IsOwner)
                {
                    continue;
                }

                _playerMovement = candidate;
                _player = candidate.Player;
                _initialCapacity = Mathf.Max(
                    _player != null ? _player.Capacity : 0f,
                    0.01f);
                return _player != null;
            }

            return false;
        }
    }
}
