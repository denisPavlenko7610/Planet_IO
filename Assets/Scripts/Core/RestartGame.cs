using UnityEngine;
using UnityEngine.SceneManagement;

namespace Planet_IO
{
    public class RestartGame : MonoBehaviour
    {
        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
    }
}
