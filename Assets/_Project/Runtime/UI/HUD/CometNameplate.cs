using TMPro;
using UnityEngine;

namespace PlanetIO.UI.Hud
{
    [RequireComponent(typeof(Comet))]
    public sealed class CometNameplate : MonoBehaviour
    {
        private const float MinimumParentScale = 0.0001f;

        [SerializeField] private TextMeshPro _label;
        [SerializeField, Range(0.02f, 0.5f)] private float _orthographicFraction = 0.105f;
        [SerializeField, Min(0f)] private float _edgePadding = 0.12f;

        private Comet _comet;
        private SpriteRenderer _cometRenderer;
        private Transform _labelTransform;
        private UnityEngine.Camera _camera;
        private bool _hasName;
        private ulong _boundSeed;

        private void Awake()
        {
            _comet = GetComponent<Comet>();
            _cometRenderer = GetComponentInChildren<SpriteRenderer>();
            _labelTransform = _label.transform;
            SyncSortingLayer();
        }

        private void OnEnable()
        {
            _hasName = false;
            _label.enabled = true;
        }

        private void SyncSortingLayer()
        {
            if (_cometRenderer == null)
            {
                return;
            }

            Renderer labelRenderer = _label.GetComponent<Renderer>();
            if (labelRenderer != null)
            {
                labelRenderer.sortingLayerID = _cometRenderer.sortingLayerID;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null)
            {
                _camera = UnityEngine.Camera.main;
            }

            if (_camera == null)
            {
                return;
            }

            ulong seed = _comet.IsSpawned ? _comet.NetworkObjectId : 0UL;
            if (!_hasName || seed != _boundSeed)
            {
                _label.text = CometNames.Get(seed);
                _boundSeed = seed;
                _hasName = true;
            }

            float labelScale = _camera.orthographicSize * _orthographicFraction;
            float parentScale = transform.lossyScale.x;
            float radius = _cometRenderer == null
                ? _comet.Capacity
                : _cometRenderer.localBounds.extents.y * _cometRenderer.transform.lossyScale.y;
            float labelHalfHeight = _label.preferredHeight * 0.5f * labelScale;
            _labelTransform.SetPositionAndRotation(
                transform.position + Vector3.up * (radius + _edgePadding + labelHalfHeight),
                Quaternion.identity);

            if (parentScale > MinimumParentScale)
            {
                _labelTransform.localScale = Vector3.one * (labelScale / parentScale);
            }
        }
    }
}
