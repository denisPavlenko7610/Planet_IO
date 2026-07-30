using UnityEngine;

namespace PlanetIO.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class PlayerDirectionArrow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _arrowRenderer;
        [SerializeField] private float _angleOffset = 90f;
        [SerializeField] private float _distanceFromCenter = 1.4f;

        private PlayerMovement _playerMovement;
        private Transform _arrowTransform;
        private bool _isVisible;

        private void Awake()
        {
            _arrowTransform = _arrowRenderer != null ? _arrowRenderer.transform : transform;
            Hide();
        }

        public void Bind(PlayerMovement playerMovement)
        {
            _playerMovement = playerMovement;
        }

        public void Hide()
        {
            _isVisible = false;

            if (_arrowRenderer != null)
            {
                _arrowRenderer.enabled = false;
            }
        }

        public void Show()
        {
            _isVisible = true;

            if (_arrowRenderer != null)
            {
                _arrowRenderer.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (!_isVisible ||
                _playerMovement == null ||
                _arrowRenderer == null)
            {
                return;
            }

            Vector2 direction = _playerMovement.Direction.normalized;
            if (direction == Vector2.zero)
            {
                direction = transform.parent != null ? transform.parent.right : Vector2.right;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + _angleOffset;
            _arrowTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            _arrowTransform.localPosition = (Vector3)(direction * _distanceFromCenter);
        }
    }
}
