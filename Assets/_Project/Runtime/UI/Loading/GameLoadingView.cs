using PlanetIO.Core.Contracts.Loading;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Loading
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameLoadingView : MonoBehaviour, IGameLoadingView
    {
        private VisualElement _overlay;

        private void OnEnable()
        {
            _overlay = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("loading-overlay");
        }

        public void Show()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
            }
        }

        public void Hide()
        {
            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
            }
        }
    }
}
