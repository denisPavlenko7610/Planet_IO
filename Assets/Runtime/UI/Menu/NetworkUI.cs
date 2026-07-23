using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Menu
{
    public interface INetworkMenuView
    {
        event Action HostRequested;
        event Action ClientRequested;

        void SetInteractionEnabled(bool interactionEnabled);
    }

    public sealed class NetworkUI : MonoBehaviour, INetworkMenuView
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        public event Action HostRequested;
        public event Action ClientRequested;

        private void OnEnable()
        {
            _hostButton.onClick.AddListener(OnHostRequested);
            _clientButton.onClick.AddListener(OnClientRequested);
        }

        private void OnDisable()
        {
            _hostButton.onClick.RemoveListener(OnHostRequested);
            _clientButton.onClick.RemoveListener(OnClientRequested);
        }

        public void SetInteractionEnabled(bool interactionEnabled)
        {
            _hostButton.interactable = interactionEnabled;
            _clientButton.interactable = interactionEnabled;
        }

        private void OnHostRequested()
        {
            HostRequested?.Invoke();
        }

        private void OnClientRequested()
        {
            ClientRequested?.Invoke();
        }
    }
}
