using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public sealed class NicknameInputView : MonoBehaviour, INicknameInputView
    {
        private const float SelectedSwatchScale = 1.2f;

        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _setRandomNicknameButton;
        [SerializeField] private RectTransform _swatchContainer;
        [SerializeField, Min(16f)] private float _swatchSize = 64f;
        [SerializeField] private Sprite _swatchSprite;

        public event Action<string> NicknameChanged;
        public event Action RandomNicknameRequested;
        public event Action<Color32> ColorSelected;

        private readonly List<Image> _swatches = new();

        private void OnEnable()
        {
            if (_inputField == null || _setRandomNicknameButton == null)
            {
                GameLogger.LogError($"{nameof(NicknameInputView)} is not configured.", this);
                enabled = false;
                return;
            }

            _inputField.characterLimit = NicknameRules.MaximumLength;
            _inputField.onEndEdit.AddListener(OnNicknameChanged);
            _setRandomNicknameButton.onClick.AddListener(OnRandomNicknameRequested);
        }

        private void OnDisable()
        {
            if (_inputField == null || _setRandomNicknameButton == null)
            {
                return;
            }

            _inputField.onEndEdit.RemoveListener(OnNicknameChanged);
            _setRandomNicknameButton.onClick.RemoveListener(OnRandomNicknameRequested);
        }

        private void Awake()
        {
            CreateColorSwatches();
        }

        public void ShowNickname(string nickname)
        {
            _inputField?.SetTextWithoutNotify(NicknameRules.Normalize(nickname));
        }

        public void ShowSelectedColor(Color32 color)
        {
            int index = PlayerPalette.IndexOf(color);
            if (index >= 0)
            {
                HighlightSwatch(index);
            }
        }

        private void OnNicknameChanged(string nickname)
        {
            string normalized = NicknameRules.Normalize(nickname);
            _inputField.SetTextWithoutNotify(normalized);
            NicknameChanged?.Invoke(normalized);
        }

        private void OnRandomNicknameRequested()
        {
            RandomNicknameRequested?.Invoke();
        }

        private void CreateColorSwatches()
        {
            if (_swatchContainer == null)
            {
                return;
            }

            Transform rowTransform = _swatchContainer;

            for (int index = 0; index < PlayerPalette.Colors.Count; index++)
            {
                Color32 color = PlayerPalette.Colors[index];
                int swatchIndex = index;

                GameObject swatchObject = new($"Swatch_{index}", typeof(RectTransform));
                swatchObject.transform.SetParent(rowTransform, false);

                Image image = swatchObject.AddComponent<Image>();
                image.sprite = _swatchSprite;
                image.color = color;

                LayoutElement element = swatchObject.AddComponent<LayoutElement>();
                element.preferredWidth = _swatchSize;
                element.preferredHeight = _swatchSize;

                Button button = swatchObject.AddComponent<Button>();
                button.onClick.AddListener(() => OnSwatchClicked(swatchIndex, color));

                _swatches.Add(image);
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
                _swatches[i].transform.localScale = i == index
                    ? new Vector3(SelectedSwatchScale, SelectedSwatchScale, 1f)
                    : Vector3.one;
            }
        }
    }
}
