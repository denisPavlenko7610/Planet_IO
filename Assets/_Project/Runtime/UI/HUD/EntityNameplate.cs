using RD.Attributes;
using TMPro;
using UnityEngine;

namespace PlanetIO.UI.Hud
{
	[RequireComponent(typeof(PlanetBody))]
    public sealed class EntityNameplate : MonoBehaviour
    {
        private const float MinimumParentScale = 0.0001f;
        private const float LabelReach = 3f;

        [SerializeField] private TextMeshPro _label;
        [SerializeField, Assign] private SpriteRenderer _entityRenderer;
        [SerializeField, Min(0f)] private float _edgePadding = 0.35f;

		private PlanetBody _entity;
        private Transform _labelTransform;
		private UnityEngine.Camera _camera;

        private void Awake()
        {
			_entity = GetComponent<PlanetBody>();
            _labelTransform = _label.transform;
            if (_entityRenderer == null)
            {
                _entityRenderer = GetComponent<SpriteRenderer>();
            }

            SyncSortingLayer();
            Bind(_entity.DisplayName);
            _entity.DisplayNameChanged += Bind;
        }

        private void SyncSortingLayer()
        {
            if (_entityRenderer == null)
            {
                return;
            }

            Renderer labelRenderer = _label.GetComponent<Renderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sortingLayerID = _entityRenderer.sortingLayerID;
            }
        }

        private void OnDestroy()
        {
            if (_entity != null)
            {
                _entity.DisplayNameChanged -= Bind;
            }
        }

        private void Bind(string displayName)
        {
            _label.text = displayName;
        }

        private void LateUpdate()
        {
			float zoomRatio = ResolveZoomRatio();

            if (!ViewBounds.IsVisible(transform.position, _entity.BodyRadius + LabelReach * zoomRatio))
            {
                return;
            }

            bool visible = _entity is not Player { IsDefeated: true };
            if (_label.enabled != visible)
            {
                _label.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            float parentScale = transform.lossyScale.x;
			float sizeFactor = NameplateRules.LabelScale * zoomRatio;
			float labelHalfHeight = _label.preferredHeight * 0.5f * sizeFactor;
			float gap = _edgePadding * zoomRatio;

			_labelTransform.SetPositionAndRotation(
				transform.position + Vector3.up * (PlanetRadius(parentScale) + gap + labelHalfHeight),
				Quaternion.identity);

            if (parentScale > MinimumParentScale)
            {
                _labelTransform.localScale = Vector3.one * (sizeFactor / parentScale);
            }
        }

		private float ResolveZoomRatio()
		{
			if (_camera == null)
			{
				_camera = UnityEngine.Camera.main;
			}

			return _camera != null ? NameplateRules.ZoomRatio(_camera.orthographicSize) : 1f;
		}

        private float PlanetRadius(float parentScale)
        {
            if (_entityRenderer == null)
            {
                return _entity.Capacity;
            }

            return _entityRenderer.localBounds.extents.y * parentScale;
        }
    }
}
