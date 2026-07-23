using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Planet_IO
{
    public sealed class NicknameInputView : MonoBehaviour
    {
        private static readonly string[] RandomNicknames =
        {
            "Bob",
            "Tom",
            "Riki",
            "Rock",
            "Margaret",
            "Monika"
        };

        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _setRandomNickname;

        public string Nickname { get; private set; }

        private void OnEnable()
        {
            if (_setRandomNickname != null)
            {
                _setRandomNickname.onClick.AddListener(SetRandomNickname);
            }

            if (_inputField != null)
            {
                _inputField.onEndEdit.AddListener(SetNickname);
            }

            SetRandomNickname();
        }

        private void OnDisable()
        {
            if (_setRandomNickname != null)
            {
                _setRandomNickname.onClick.RemoveListener(SetRandomNickname);
            }

            if (_inputField != null)
            {
                _inputField.onEndEdit.RemoveListener(SetNickname);
            }
        }

        private void SetNickname(string nickname)
        {
            Nickname = nickname?.Trim() ?? string.Empty;
        }

        private void SetRandomNickname()
        {
            int index = Random.Range(0, RandomNicknames.Length);
            SetNickname(RandomNicknames[index]);

            if (_inputField != null)
            {
                _inputField.SetTextWithoutNotify(Nickname);
            }
        }
    }
}
