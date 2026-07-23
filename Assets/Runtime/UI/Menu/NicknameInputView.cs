using System;
using Planet_IO;
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
            _inputField.characterLimit = NicknameRules.MaximumLength;
            _inputField.onEndEdit.AddListener(OnNicknameChanged);
            _setRandomNicknameButton.onClick.AddListener(
                OnRandomNicknameRequested);
        }

        private void OnDisable()
        {
            _inputField.onEndEdit.RemoveListener(OnNicknameChanged);
            _setRandomNicknameButton.onClick.RemoveListener(
                OnRandomNicknameRequested);
        }

        public void ShowNickname(string nickname)
        {
            _inputField.SetTextWithoutNotify(nickname);
        }

        private void OnNicknameChanged(string nickname)
        {
            NicknameChanged?.Invoke(nickname);
        }

        private void OnRandomNicknameRequested()
        {
            RandomNicknameRequested?.Invoke();
        }
    }
}
