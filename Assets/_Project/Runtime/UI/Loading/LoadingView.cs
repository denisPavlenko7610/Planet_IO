using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PlanetIO.UI.Loading
{
    public interface ILoadingView
    {
        void Render(float progress, string status);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class LoadingView : MonoBehaviour, ILoadingView
    {
        private ILocalizationService _localization;
        private LocalizedLabels _labels;
        private VisualElement _progressFill;
        private Label _statusText;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        private void OnDisable()
        {
            _labels?.Dispose();
            _labels = null;
        }

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _progressFill = root.Q<VisualElement>("progress-fill");
            _statusText = root.Q<Label>("loading-status");
            _labels?.Dispose();
            _labels = new LocalizedLabels(_localization);
            _labels.BindLanguageClass(root);
            _labels.Bind(root.Q<Label>("loading-title"), LocalizationKeys.StatusLoading);
        }

        public void Render(float progress, string status)
        {
            float normalizedProgress = Mathf.Clamp01(progress);
            if (_progressFill != null)
            {
                _progressFill.style.width = Length.Percent(normalizedProgress * 100f);
            }

            if (_statusText != null)
            {
                _statusText.text = $"{status ?? string.Empty}  {normalizedProgress:P0}";
            }
        }
    }
}
