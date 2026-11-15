using System;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Menu
{
    public interface IModeView
    {
        event Action<bool> BlitzChanged;

        void ShowBlitz(bool enabled, string label);
    }

    public sealed partial class MainMenuView : IModeView
    {
        private Toggle _blitzToggle;
        private Label _blitzLabel;

        public event Action<bool> BlitzChanged;

        private void EnableModeView(VisualElement root)
        {
            _blitzToggle = root.Q<Toggle>("blitz-toggle");
            _blitzLabel = root.Q<Label>("blitz-label");
            _blitzToggle.RegisterValueChangedCallback(OnBlitzToggled);
        }

        private void DisableModeView()
        {
            _blitzToggle.UnregisterValueChangedCallback(OnBlitzToggled);
        }

        public void ShowBlitz(bool enabled, string label)
        {
            _blitzToggle.SetValueWithoutNotify(enabled);
            _blitzLabel.text = label ?? string.Empty;
        }

        private void OnBlitzToggled(ChangeEvent<bool> changeEvent)
        {
            BlitzChanged?.Invoke(changeEvent.newValue);
        }
    }
}
