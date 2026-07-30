using System;
using PlanetIO.UI.Mobile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Menu
{
    public interface INetworkMenuView
    {
        event Action HostRequested;
        event Action<string> JoinRequested;
        event Action SinglePlayerRequested;

        void SetInteractionEnabled(bool interactionEnabled);
        void ShowStatus(string status, bool isError);
    }

    public sealed class NetworkUI : MonoBehaviour, INetworkMenuView
    {
        private static readonly Color ErrorColor = new(1f, 0.38f, 0.38f);
        private static readonly Color InfoColor = new(0.75f, 0.9f, 1f);

        [Header("Menu controls")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;
        [SerializeField] private Button _singlePlayerButton;
        [SerializeField] private TMP_InputField _joinCodeInput;
        [SerializeField] private TMP_Text _statusText;

        public event Action HostRequested;
        public event Action<string> JoinRequested;
        public event Action SinglePlayerRequested;

        private void Awake()
        {
            SafeAreaFitter.AttachTo(transform.parent);
        }

        private void OnEnable()
        {
            _hostButton.onClick.AddListener(OnHostRequested);
            _clientButton.onClick.AddListener(OnJoinRequested);
            _singlePlayerButton.onClick.AddListener(OnSinglePlayerRequested);
        }

        private void OnDisable()
        {
            _hostButton.onClick.RemoveListener(OnHostRequested);
            _clientButton.onClick.RemoveListener(OnJoinRequested);
            _singlePlayerButton.onClick.RemoveListener(OnSinglePlayerRequested);
        }

        public void SetInteractionEnabled(bool interactionEnabled)
        {
            SetInteractable(_hostButton, interactionEnabled);
            SetInteractable(_clientButton, interactionEnabled);
            SetInteractable(_singlePlayerButton, interactionEnabled);
            SetInteractable(_joinCodeInput, interactionEnabled);
        }

        public void ShowStatus(string status, bool isError)
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.text = status ?? string.Empty;
            _statusText.color = isError ? ErrorColor : InfoColor;
        }

        private void OnHostRequested()
        {
            HostRequested?.Invoke();
        }

        private void OnJoinRequested()
        {
            string joinCode = _joinCodeInput?.text;
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                ShowStatus("Enter room code", true);
                return;
            }

            JoinRequested?.Invoke(joinCode.Trim());
        }

        private void OnSinglePlayerRequested()
        {
            SinglePlayerRequested?.Invoke();
        }

        private static void SetInteractable(
            Selectable selectable,
            bool interactable)
        {
            if (selectable != null)
            {
                selectable.interactable = interactable;
            }
        }
    }
}
