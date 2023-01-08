using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Planet_IO
{
    public class SaveData : MonoBehaviour
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _setRandomNickname;

        private List<string> _inputNames = new () { "Bob", "Tom", "Riki", "Rock", "Margaret", "Monika" };
        private string _nickname;

        private void OnEnable()
        {
            _setRandomNickname.onClick.AddListener(SetRandomNickname);
        }

        private void OnDisable()
        {
            _setRandomNickname.onClick.RemoveListener(SetRandomNickname);
        }

        private void Start()
        {            
            _inputField.onEndEdit.AddListener(_nickname => SetInput(_inputField));
        }

        private void SetInput(TMP_InputField userInput)
        {
            _nickname = userInput.text;            
        }

        private void SetRandomNickname() 
        {
            int arrayCount = _inputNames.Count;
            int numberElement = Random.Range(0, arrayCount);

            _inputField.text = _inputNames[numberElement];            
        }
    }
}