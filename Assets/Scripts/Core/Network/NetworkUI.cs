using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Planet_IO.Core.Network
{
    public class NetworkUI : NetworkBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private TextMeshProUGUI _playerText;

        private NetworkVariable<int> playerNum = new(0,NetworkVariableReadPermission.Everyone);

        private void Awake()
        {
            Subscribe();
        }

        private void Subscribe()
        {
            _hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); });
            _clientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); });
        }

        private void Update()
        {
            UpdatePlayersCount();
        }

        private void UpdatePlayersCount()
        {
            _playerText.text = "Players: " + playerNum.Value;

            if (!IsServer)
                return;

            playerNum.Value = NetworkManager.Singleton.ConnectedClients.Count;
        }
    }
}