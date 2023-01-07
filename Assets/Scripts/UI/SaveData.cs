using TMPro;
using UnityEngine;

namespace Planet_IO
{
    public class SaveData : MonoBehaviour
    {
        [SerializeField] public TMP_InputField _inputField;

        private string _nickname;

        void Start()
        {
            _inputField.onEndEdit.AddListener(delegate { SetInput(_inputField); });
        }

        private void SetInput(TMP_InputField userInput)
        {
            string _nickname = userInput.text;            
        }
    }
}