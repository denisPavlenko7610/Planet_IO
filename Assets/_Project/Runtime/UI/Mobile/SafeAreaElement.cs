using UnityEngine;

namespace PlanetIO.UI.Mobile
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaElement : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Vector2 _basePosition;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            _basePosition = _rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            SafeAreaHelper.HasScreenChanged(ref _lastSafeArea, ref _lastScreenSize);
            Apply();
        }

        private void Update()
        {
            if (SafeAreaHelper.HasScreenChanged(ref _lastSafeArea, ref _lastScreenSize))
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rectTransform == null ||
                Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            float scaleFactor = Mathf.Max(_canvas != null ? _canvas.scaleFactor : 1f, 0.01f);
            float left = safeArea.xMin / scaleFactor;
            float right = (Screen.width - safeArea.xMax) / scaleFactor;
            float bottom = safeArea.yMin / scaleFactor;
            float top = (Screen.height - safeArea.yMax) / scaleFactor;

            Vector2 offset = Vector2.zero;
            if (_rectTransform.anchorMax.x <= 0.5f)
            {
                offset.x += left;
            }
            else if (_rectTransform.anchorMin.x >= 0.5f)
            {
                offset.x -= right;
            }

            if (_rectTransform.anchorMax.y <= 0.5f)
            {
                offset.y += bottom;
            }
            else if (_rectTransform.anchorMin.y >= 0.5f)
            {
                offset.y -= top;
            }

            _rectTransform.anchoredPosition = _basePosition + offset;
        }
    }
}
