using UnityEngine;

namespace PlanetIO.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class PlayerDirectionArrow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _arrowRenderer;
        [SerializeField] private float _angleOffset = 90f;
        [SerializeField, Range(0.01f, 0.1f)] private float _screenHeightFraction = 0.035f;
        [SerializeField, Range(0f, 2f)] private float _edgeGapInArrowSizes = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _alpha = 0.85f;

        private PlayerMovement _playerMovement;
        private Transform _arrowTransform;
        private UnityEngine.Camera _camera;
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

            Player player = _playerMovement.Player;
            bool shown = player != null && !player.IsDefeated;
            if (_arrowRenderer.enabled != shown)
            {
                _arrowRenderer.enabled = shown;
            }

            if (!shown)
            {
                return;
            }

            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }

            Transform parent = _arrowTransform.parent;
            float parentScale = parent != null ? Mathf.Abs(parent.lossyScale.x) : 1f;
            Sprite sprite = _arrowRenderer.sprite;
            if (_camera == null || sprite == null || parentScale < 0.0001f)
            {
                return;
            }

            Vector2 direction = _playerMovement.Direction.normalized;
            if (direction == Vector2.zero)
            {
                direction = parent != null ? parent.right : Vector2.right;
            }

            Vector2 localDirection = parent != null
                ? (Vector2)parent.InverseTransformDirection(direction)
                : direction;

            if (localDirection.sqrMagnitude < 0.0001f)
            {
                return;
            }

            localDirection.Normalize();

            float worldSize = DirectionArrowRules.WorldSize(_camera.orthographicSize, _screenHeightFraction);
            float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y, 0.0001f);
            float distance = DirectionArrowRules.Distance(player.BodyRadius, worldSize, _edgeGapInArrowSizes);

            float scale = worldSize / spriteSize / parentScale;
            _arrowTransform.localScale = new Vector3(scale, scale, 1f);
            _arrowTransform.localPosition = (Vector3)(localDirection * (distance / parentScale));

            float angle = Mathf.Atan2(localDirection.y, localDirection.x) * Mathf.Rad2Deg + _angleOffset;
            _arrowTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            Color color = _arrowRenderer.color;
            color.a = _alpha;
            _arrowRenderer.color = color;
        }
    }
}
