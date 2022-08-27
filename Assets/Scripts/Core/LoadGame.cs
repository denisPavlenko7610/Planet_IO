using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Planet_IO
{
    public class LoadGame : MonoBehaviour
    {
        [SerializeField] Button _startButton;

        void Start()
        {
            _startButton.onClick.AddListener(StartButtonPressed);
        }

        void StartButtonPressed()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        private void OnDisable()
        {
            _startButton.onClick.RemoveListener(StartButtonPressed);
        }
    }
}
