using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace PlanetIO_Core
{
    public class LoadGame : MonoBehaviour
    {
        [SerializeField] private UIDocument _uiDocument;
        
        private Button _startButton;
        string playbutton = "PlayButton";

        void Start()
        {
            var root = _uiDocument.rootVisualElement;
            _startButton = root.Q<Button>(playbutton);
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
