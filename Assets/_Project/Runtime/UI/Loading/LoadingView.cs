using PlanetIO.UI.Mobile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Loading
{
    public interface ILoadingView
    {
        void Render(float progress, string status);
    }

    public sealed class LoadingView : MonoBehaviour, ILoadingView
    {
        private const float ReferenceWidth = 1920f;
        private const float ReferenceHeight = 1080f;

        private Image _progressFill;
        private TMP_Text _statusText;

        private void Awake()
        {
            BuildResponsiveView();
        }

        public void Render(float progress, string status)
        {
            float normalizedProgress = Mathf.Clamp01(progress);
            if (_progressFill != null)
            {
                _progressFill.fillAmount = normalizedProgress;
            }

            if (_statusText != null)
            {
                _statusText.text =
                    $"{status ?? string.Empty}  {normalizedProgress:P0}";
            }
        }

        private void BuildResponsiveView()
        {
            if (_progressFill != null)
            {
                return;
            }

            GameObject canvasObject = new("LoadingCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution =
                new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform background = CreateRect(
                "Background",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one);
            Image backgroundImage = background.gameObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.025f, 0.035f, 0.08f, 1f);

            RectTransform content = CreateRect(
                "SafeArea",
                canvasObject.transform,
                Vector2.zero,
                Vector2.one);
            SafeAreaFitter.AttachTo(content);

            TMP_Text title = CreateText(
                "Title",
                content,
                new Vector2(0.5f, 0.59f),
                new Vector2(960f, 90f),
                48f,
                FontStyles.Bold);
            title.text = "Loading Session";
            title.color = new Color(0.88f, 0.94f, 1f);

            RectTransform progressBackground = CreateRect(
                "ProgressBackground",
                content,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(960f, 38f));
            Image progressBackgroundImage =
                progressBackground.gameObject.AddComponent<Image>();
            progressBackgroundImage.color =
                new Color(0.12f, 0.17f, 0.28f, 1f);

            RectTransform fillRect = CreateRect(
                "ProgressFill",
                progressBackground,
                Vector2.zero,
                Vector2.one);
            fillRect.offsetMin = new Vector2(4f, 4f);
            fillRect.offsetMax = new Vector2(-4f, -4f);
            _progressFill = fillRect.gameObject.AddComponent<Image>();
            _progressFill.color = new Color(0.16f, 0.72f, 1f, 1f);
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillOrigin = 0;
            _progressFill.fillAmount = 0f;

            _statusText = CreateText(
                "Status",
                content,
                new Vector2(0.5f, 0.42f),
                new Vector2(1200f, 80f),
                30f,
                FontStyles.Normal);
            _statusText.color = new Color(0.72f, 0.82f, 0.94f);
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 size,
            float fontSize,
            FontStyles fontStyle)
        {
            RectTransform rectTransform = CreateRect(
                name,
                parent,
                anchor,
                anchor,
                size);
            TMP_Text text = rectTransform.gameObject
                .AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = Mathf.Max(16f, fontSize * 0.55f);
            text.fontSizeMax = fontSize;
            return text;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2? size = null)
        {
            GameObject gameObject = new(
                name,
                typeof(RectTransform));
            RectTransform rectTransform =
                gameObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = size ?? Vector2.zero;
            return rectTransform;
        }
    }
}
