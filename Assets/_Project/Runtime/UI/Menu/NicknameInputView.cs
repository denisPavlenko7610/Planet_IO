using RD.Foundation;
using RD.Localization;
using System;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PlanetIO.UI.Menu
{
    public interface INicknameInputView
    {
        event Action<string> NicknameChanged;
        event Action RandomNicknameRequested;

        void ShowNickname(string nickname);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class NicknameInputView : MonoBehaviour, INicknameInputView
    {
        private ILocalizationService _localization;
        private LocalizedLabels _labels;
        private TextField _inputField;
        private Button _setRandomNicknameButton;
        private Label _profileName;

        public event Action<string> NicknameChanged;
        public event Action RandomNicknameRequested;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _inputField = root.Q<TextField>("nickname-input");
            _setRandomNicknameButton = root.Q<Button>("random-nickname");
            _profileName = root.Q<Label>("profile-name");

            if (_inputField == null || _setRandomNicknameButton == null)
            {
                UnityLogger.LogError($"{nameof(NicknameInputView)} is not configured.", this);
                enabled = false;
                return;
            }

            _inputField.maxLength = NicknameRules.MaximumLength;
            _inputField.isDelayed = true;
            _inputField.RegisterValueChangedCallback(OnNicknameEdited);
            _setRandomNicknameButton.clicked += OnRandomNicknameRequested;


            _labels?.Dispose();
            _labels = new LocalizedLabels(_localization);
            _labels.BindPlaceholder(_inputField, LocalizationKeys.MenuNicknamePlaceholder);
        }

        private void OnDisable()
        {
            if (_inputField == null || _setRandomNicknameButton == null)
            {
                return;
            }

            _inputField.UnregisterValueChangedCallback(OnNicknameEdited);
            _setRandomNicknameButton.clicked -= OnRandomNicknameRequested;
            _labels?.Dispose();
            _labels = null;
        }

        public void ShowNickname(string nickname)
        {
            string normalized = NicknameRules.Normalize(nickname);
            _inputField?.SetValueWithoutNotify(normalized);
            if (_profileName != null)
            {
                _profileName.text = normalized;
            }
        }

        private void OnNicknameEdited(ChangeEvent<string> changeEvent)
        {
            string normalized = NicknameRules.Normalize(changeEvent.newValue);
            _inputField.SetValueWithoutNotify(normalized);
            NicknameChanged?.Invoke(normalized);
        }

        private void OnRandomNicknameRequested()
        {
            RandomNicknameRequested?.Invoke();
        }

    }
}
