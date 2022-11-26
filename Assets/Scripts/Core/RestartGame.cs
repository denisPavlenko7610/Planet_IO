using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlanetIO_Core
{
    public class RestartGame : MonoBehaviour
    {
        private void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}