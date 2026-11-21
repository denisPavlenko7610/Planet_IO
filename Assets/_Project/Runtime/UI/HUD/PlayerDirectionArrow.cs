using UnityEngine;

namespace PlanetIO.UI.Hud
{
    [DisallowMultipleComponent]
    public sealed class PlayerDirectionArrow : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _arrowRenderer;
		[SerializeField] private float _angleOffset = 0f;
		[SerializeField, Range(0.01f, 0.1f)]
		private float _screenHeightFraction = DirectionArrowRules.ScreenHeightFraction;
        [SerializeField, Range(0f, 2f)] private float _edgeGapInArrowSizes = 0.5f;
		[SerializeField, Range(0f, 1f)] private float _alpha = 0.75f;

		private SpriteRenderer _bodyRenderer;
		private LeaderBeaconView _beacon;

        private PlayerMovement _playerMovement;
        private Transform _arrowTransform;
        private UnityEngine.Camera _camera;
        private bool _isVisible;

        private void Awake()
        {
			if (_arrowRenderer != null)
			{
				_arrowRenderer.sprite = OverlaySprites.Chevron;
				_arrowRenderer.color = new Color(1f, 1f, 1f, _alpha);
			}

            _arrowTransform = _arrowRenderer != null ? _arrowRenderer.transform : transform;
            Hide();
        }

        public void Bind(PlayerMovement playerMovement)
        {
            _playerMovement = playerMovement;
			_bodyRenderer = playerMovement != null ? playerMovement.Player.GetComponent<SpriteRenderer>() : null;
			_beacon = playerMovement != null ? playerMovement.Player.GetComponent<LeaderBeaconView>() : null;
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
			float spriteRadius = _bodyRenderer != null
				? Mathf.Max(_bodyRenderer.bounds.extents.x, _bodyRenderer.bounds.extents.y) +
				Vector2.Distance(_bodyRenderer.bounds.center, player.transform.position)
				: 0f;
			float beaconRadius = _beacon != null ? _beacon.OuterWorldRadius : 0f;
			float radius = DirectionArrowRules.ClearanceRadius(player.BodyRadius, spriteRadius, beaconRadius);
			float distance = DirectionArrowRules.Distance(radius, worldSize, _edgeGapInArrowSizes);

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
