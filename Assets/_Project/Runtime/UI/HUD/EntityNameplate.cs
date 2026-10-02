using PlanetIO.Core.Attributes;
using TMPro;
using UnityEngine;

namespace PlanetIO.UI.Hud
{
    [RequireComponent(typeof(PlanetScale))]
    public sealed class EntityNameplate : MonoBehaviour
    {
        private const float MinimumParentScale = 0.0001f;
        private const float MinimumLabelScale = 0.45f;
        private const float MaximumLabelScale = 1.6f;

        [SerializeField] private TextMeshPro _label;
        [SerializeField, Assign] private SpriteRenderer _entityRenderer;
        [SerializeField, Min(0f)] private float _edgePadding = 0.35f;

        private PlanetScale _entity;
        private Transform _labelTransform;

        private void Awake()
        {
            _entity = GetComponent<PlanetScale>();
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
            float sizeFactor = Mathf.Clamp(MinimumLabelScale + _entity.Capacity, MinimumLabelScale, MaximumLabelScale);
            float labelHalfHeight = _label.preferredHeight * 0.5f * sizeFactor;
            _labelTransform.SetPositionAndRotation(
                transform.position + Vector3.up * (PlanetRadius(parentScale) + _edgePadding + labelHalfHeight),
                Quaternion.identity);

            if (parentScale > MinimumParentScale)
            {
                _labelTransform.localScale = Vector3.one * (sizeFactor / parentScale);
            }
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
