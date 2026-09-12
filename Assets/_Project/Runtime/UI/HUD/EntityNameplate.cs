using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Hud
{
    [RequireComponent(typeof(PlanetScale))]
    public sealed class EntityNameplate : MonoBehaviour
    {
        private const float CanvasWidth = 320f;
        private const float CanvasHeight = 90f;
        private const float CanvasToWorldScale = 0.012f;
        private const float MinimumParentScale = 0.0001f;

        [SerializeField, Min(0f)] private float _edgePadding = 0.35f;
        [SerializeField] private Color _textColor = new(1f, 1f, 1f, 0.92f);
        [SerializeField] private Color _outlineColor = new(0f, 0f, 0f, 0.85f);

        private PlanetScale _entity;
        private RectTransform _canvasRectTransform;
        private Text _label;

        private void Awake()
        {
            _entity = GetComponent<PlanetScale>();
            BuildCanvas();
            Bind(_entity.DisplayName);
            _entity.DisplayNameChanged += Bind;
        }

        private void OnDestroy()
        {
            if (_entity != null)
            {
                _entity.DisplayNameChanged -= Bind;
            }
        }

        public void Hide()
        {
            if (_canvasRectTransform != null)
            {
                _canvasRectTransform.gameObject.SetActive(false);
            }
        }

        private void Bind(string displayName)
        {
            if (_label != null)
            {
                _label.text = displayName;
            }
        }

        private void LateUpdate()
        {
            if (_canvasRectTransform == null)
            {
                return;
            }

            if (_entity is Player { IsDefeated: true })
            {
                Hide();
                return;
            }

            if (!_canvasRectTransform.gameObject.activeSelf)
            {
                _canvasRectTransform.gameObject.SetActive(true);
            }

            Transform entityTransform = transform;
            float parentScale = entityTransform.lossyScale.x;

            _canvasRectTransform.position =
                entityTransform.position + Vector3.up * (_entity.Capacity + _edgePadding);
            _canvasRectTransform.rotation = Quaternion.identity;

            if (parentScale > MinimumParentScale)
            {
                _canvasRectTransform.localScale =
                    Vector3.one * (CanvasToWorldScale / parentScale);
            }
        }

        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject("NameplateCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 500;

            _canvasRectTransform = canvasObject.GetComponent<RectTransform>();
            _canvasRectTransform.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);
            _canvasRectTransform.pivot = new Vector2(0.5f, 0f);
            _canvasRectTransform.anchorMin = Vector2.zero;
            _canvasRectTransform.anchorMax = Vector2.zero;
            _canvasRectTransform.anchoredPosition = Vector2.zero;

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(_canvasRectTransform, false);

            _label = labelObject.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 44;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = _textColor;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;

            RectTransform labelRect = _label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Outline outline = labelObject.AddComponent<Outline>();
            outline.effectColor = _outlineColor;
            outline.effectDistance = new Vector2(1.6f, -1.6f);
        }
    }
}
