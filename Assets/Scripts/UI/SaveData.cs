using TMPro;
using UnityEngine;

namespace Planet_IO
{
    public class SaveData : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;

        private string _nickname;

        private void Start()
        {
            _inputField.onEndEdit.AddListener(_nickname => SetInput(_inputField) );
        }

        private void SetInput(TMP_InputField userInput)
        {
            _nickname = userInput.text;
            Debug.Log(_nickname);
        }
    }
}