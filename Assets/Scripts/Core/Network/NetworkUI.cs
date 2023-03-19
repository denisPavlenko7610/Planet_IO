using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Planet_IO.Core.Network
{
    public class NetworkUI : NetworkBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        private void Awake()
        {
            Subscribe();
        }

        private void Subscribe()
        {
            _hostButton.onClick.AddListener(async () => await LoadHost());
            _clientButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartClient();
            });
        }

        private static async Task LoadHost()
        {
            await SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);
            NetworkManager.Singleton.StartHost();
        }
    }
}