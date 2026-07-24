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

        public static void AttachTo(RectTransform target)
        {
            if (target != null &&
                target.GetComponent<SafeAreaElement>() == null)
            {
                target.gameObject.AddComponent<SafeAreaElement>();
            }
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();
            _basePosition = _rectTransform.anchoredPosition;
            Apply();
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height)
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
            float scaleFactor = Mathf.Max(
                _canvas != null ? _canvas.scaleFactor : 1f,
                0.01f);
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
            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }
    }
}
