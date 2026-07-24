using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Menu
{
    public interface INicknameInputView
    {
        event Action<string> NicknameChanged;
        event Action RandomNicknameRequested;

        void ShowNickname(string nickname);
    }

    public sealed class NicknameInputView : MonoBehaviour, INicknameInputView
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _setRandomNicknameButton;

        public event Action<string> NicknameChanged;
        public event Action RandomNicknameRequested;

        private void OnEnable()
        {
            if (_inputField == null || _setRandomNicknameButton == null)
            {
                LoggerIO.LogError($"{nameof(NicknameInputView)} is not configured.", this);
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

        public void ShowNickname(string nickname)
        {
            _inputField?.SetTextWithoutNotify(NicknameRules.Normalize(nickname));
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
    }
}
