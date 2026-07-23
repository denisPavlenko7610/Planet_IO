using Planet_IO;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace PlanetIO.UI.Menu
{
    public sealed class NetworkUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        private INetworkSessionService _session;
        private bool _requestInProgress;

        [Inject]
        public void Construct(INetworkSessionService session)
        {
            _session = session;
        }

        private void Awake()
        {
            _hostButton.onClick.AddListener(OnHostClicked);
            _clientButton.onClick.AddListener(OnClientClicked);
        }

        private void OnDestroy()
        {
            _hostButton.onClick.RemoveListener(OnHostClicked);
            _clientButton.onClick.RemoveListener(OnClientClicked);
        }

        private async void OnHostClicked()
        {
            if (_requestInProgress || _session == null)
            {
                return;
            }

            SetButtonsInteractable(false);
            _requestInProgress = true;

            bool started = await _session.StartHostAsync();
            if (!started)
            {
                _requestInProgress = false;
                SetButtonsInteractable(true);
                Debug.LogError($"Не удалось запустить хост: {_session.Status}", this);
            }
        }

        private void OnClientClicked()
        {
            if (_requestInProgress || _session == null)
            {
                return;
            }

            _requestInProgress = true;
            SetButtonsInteractable(false);

            if (!_session.StartClient())
            {
                _requestInProgress = false;
                SetButtonsInteractable(true);
                Debug.LogError($"Не удалось запустить клиент: {_session.Status}", this);
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _hostButton.interactable = interactable;
            _clientButton.interactable = interactable;
        }
    }
}
