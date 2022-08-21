using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Planet_IO
{
    public class LoadGame : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private Button _startButton;
        private string _playbutton = "PlayButton";

        void Start()
        {
            var root = _uiDocument.rootVisualElement;
            _startButton = root.Q<Button>(_playbutton);
            _startButton.clicked += StartButtonPressed;
        }

        void StartButtonPressed()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        private void OnDisable()
        {
            _startButton.clicked -= StartButtonPressed;
        }
    }
}
