using RD.Settings;
using TMPro;
using UnityEngine;
using VContainer;

namespace PlanetIO.UI.Hud
{
    [RequireComponent(typeof(Comet))]
    public sealed class CometNameplate : MonoBehaviour
    {
        private const float MinimumParentScale = 0.0001f;

        [SerializeField] private TextMeshPro _label;
        [SerializeField, Min(0.01f)] private float _labelScale = 0.5f;
        [SerializeField, Min(0f)] private float _edgePadding = 0.12f;

        private Comet _comet;
        private SpriteRenderer _cometRenderer;
        private Transform _labelTransform;
        private ISettingsService _settings;
        private bool _subscribed;
        private bool _show;
        private bool _hasName;
        private ulong _boundSeed;

        [Inject]
        public void Construct(ISettingsService settings)
        {
            _settings = settings;
            if (isActiveAndEnabled)
            {
                Subscribe();
            }
        }

        private void Awake()
        {
            _comet = GetComponent<Comet>();
            _cometRenderer = GetComponentInChildren<SpriteRenderer>();
            _labelTransform = _label.transform;
            SyncSortingLayer();
            SetVisible(false);
        }

        private void OnEnable()
        {
            _hasName = false;
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            SetVisible(false);
        }

        private void Subscribe()
        {
            if (_settings == null || _subscribed)
            {
                return;
            }

            _settings.Changed += OnSettingChanged;
            _subscribed = true;
            SetVisible(_settings.Get(GameSettingKeys.ShowCometNames));
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _settings.Changed -= OnSettingChanged;
            _subscribed = false;
        }

        private void OnSettingChanged(SettingChanged change)
        {
            if (change.Id == GameSettingKeys.ShowCometNames.Id)
            {
                SetVisible(_settings.Get(GameSettingKeys.ShowCometNames));
            }
        }

        private void SetVisible(bool visible)
        {
            _show = visible;
            if (_label != null && _label.enabled != visible)
            {
                _label.enabled = visible;
            }
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
            if (!_show)
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

            float parentScale = transform.lossyScale.x;
            float radius = _cometRenderer == null
                ? _comet.Capacity
                : _cometRenderer.localBounds.extents.y * _cometRenderer.transform.lossyScale.y;
            float labelHalfHeight = _label.preferredHeight * 0.5f * _labelScale;
            _labelTransform.SetPositionAndRotation(
                transform.position + Vector3.up * (radius + _edgePadding + labelHalfHeight),
                Quaternion.identity);

            if (parentScale > MinimumParentScale)
            {
                _labelTransform.localScale = Vector3.one * (_labelScale / parentScale);
            }
        }
    }
}
