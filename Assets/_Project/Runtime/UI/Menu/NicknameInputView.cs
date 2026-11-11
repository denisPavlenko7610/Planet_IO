using RD.Foundation;
using RD.Localization;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PlanetIO.UI.Menu
{
    public interface INicknameInputView
    {
        event Action<string> NicknameChanged;
        event Action RandomNicknameRequested;
        event Action<Color32> ColorSelected;

        void ShowNickname(string nickname);
        void ShowSelectedColor(Color32 color);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class NicknameInputView : MonoBehaviour, INicknameInputView
    {
        private const string SwatchClass = "swatch";
        private const string SwatchSelectedClass = "swatch--selected";

        private readonly List<VisualElement> _swatches = new();

        private ILocalizationService _localization;
        private LocalizedLabels _labels;
        private TextField _inputField;
        private Button _setRandomNicknameButton;

        public event Action<string> NicknameChanged;
        public event Action RandomNicknameRequested;
        public event Action<Color32> ColorSelected;

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
            VisualElement swatchContainer = root.Q<VisualElement>("swatches");

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

            CreateColorSwatches(swatchContainer);

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
            _inputField?.SetValueWithoutNotify(NicknameRules.Normalize(nickname));
        }

        public void ShowSelectedColor(Color32 color)
        {
            int index = PlayerPalette.IndexOf(color);
            if (index >= 0)
            {
                HighlightSwatch(index);
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

        private void CreateColorSwatches(VisualElement container)
        {
            _swatches.Clear();
            if (container == null)
            {
                return;
            }

            container.Clear();
            for (int index = 0; index < PlayerPalette.Colors.Count; index++)
            {
                Color32 color = PlayerPalette.Colors[index];
                int swatchIndex = index;

                VisualElement swatch = new();
                swatch.AddToClassList(SwatchClass);
                swatch.style.backgroundColor = new Color(color.r / 255f, color.g / 255f, color.b / 255f, 1f);
                swatch.RegisterCallback<ClickEvent>(_ => OnSwatchClicked(swatchIndex, color));

                container.Add(swatch);
                _swatches.Add(swatch);
            }
        }

        private void OnSwatchClicked(int index, Color32 color)
        {
            HighlightSwatch(index);
            ColorSelected?.Invoke(color);
        }

        private void HighlightSwatch(int index)
        {
            for (int i = 0; i < _swatches.Count; i++)
            {
                _swatches[i].EnableInClassList(SwatchSelectedClass, i == index);
            }
        }
    }
}
