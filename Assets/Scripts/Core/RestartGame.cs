using UnityEngine;
using Planet_IO;
using UnityEngine.SceneManagement;
using Zenject;

namespace PlanetIO_Core
{
    public class RestartGame : MonoBehaviour
    {
        private PlayerScale _playerScale;

        [Inject]
        private void Construct(PlayerScale playerScale) => _playerScale = playerScale;
        
        private void Update()
        {
          //  if (_playerScale.IsDie) 
               // Restart();
        }

        private void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}