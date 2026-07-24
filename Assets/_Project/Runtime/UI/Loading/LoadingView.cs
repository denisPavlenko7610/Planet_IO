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
        [SerializeField] private Image _progressFill;
        [SerializeField] private TMP_Text _statusText;

        public void Render(float progress, string status)
        {
            float normalizedProgress = Mathf.Clamp01(progress);
            if (_progressFill != null)
            {
                _progressFill.fillAmount = normalizedProgress;
            }

            if (_statusText != null)
            {
                _statusText.text = $"{status ?? string.Empty}  {normalizedProgress:P0}";
            }
        }
    }
}
