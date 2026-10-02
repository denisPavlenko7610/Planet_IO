using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Menu
{
    public interface IMainMenuView
    {
        event Action PlayWithBotsRequested;
        event Action CreateRoomRequested;
        event Action QuickPlayRequested;
        event Action<string> JoinRoomRequested;
        event Action SettingsRequested;

        void SetInteractionEnabled(bool interactionEnabled);
        void ShowStatus(string status, bool isError);
        void ShowBestScore(string text);
        void ShowRoomCode(string roomCode);
    }

    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        private static readonly Color ErrorColor = new(1f, 0.45f, 0.45f);
        private static readonly Color InfoColor = new(0.8f, 0.92f, 1f);

        [SerializeField] private Button _playWithBotsButton;
        [SerializeField] private Button _createRoomButton;
        [SerializeField] private Button _quickPlayButton;
        [SerializeField] private Button _joinRoomButton;
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _bestScoreText;

        public event Action PlayWithBotsRequested;
        public event Action CreateRoomRequested;
        public event Action QuickPlayRequested;
        public event Action<string> JoinRoomRequested;
        public event Action SettingsRequested;

        private void Awake()
        {
            _roomCodeInput.characterLimit = RoomRules.MaximumRoomCodeLength;
            _roomCodeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
            _roomCodeInput.onValidateInput = (_, _, character) => char.ToUpperInvariant(character);
        }

        private void OnEnable()
        {
            _playWithBotsButton.onClick.AddListener(OnPlayWithBots);
            _createRoomButton.onClick.AddListener(OnCreateRoom);
            _quickPlayButton.onClick.AddListener(OnQuickPlay);
            _joinRoomButton.onClick.AddListener(OnJoinRoom);
            _settingsButton.onClick.AddListener(OnSettings);
            _roomCodeInput.onSubmit.AddListener(OnRoomCodeSubmitted);
        }

        private void OnDisable()
        {
            _playWithBotsButton.onClick.RemoveListener(OnPlayWithBots);
            _createRoomButton.onClick.RemoveListener(OnCreateRoom);
            _quickPlayButton.onClick.RemoveListener(OnQuickPlay);
            _joinRoomButton.onClick.RemoveListener(OnJoinRoom);
            _settingsButton.onClick.RemoveListener(OnSettings);
            _roomCodeInput.onSubmit.RemoveListener(OnRoomCodeSubmitted);
        }

        public void SetInteractionEnabled(bool interactionEnabled)
        {
            _playWithBotsButton.interactable = interactionEnabled;
            _createRoomButton.interactable = interactionEnabled;
            _quickPlayButton.interactable = interactionEnabled;
            _joinRoomButton.interactable = interactionEnabled;
            _roomCodeInput.interactable = interactionEnabled;
        }

        public void ShowStatus(string status, bool isError)
        {
            _statusText.text = status ?? string.Empty;
            _statusText.color = isError ? ErrorColor : InfoColor;
        }

        public void ShowBestScore(string text)
        {
            _bestScoreText.text = text ?? string.Empty;
            _bestScoreText.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void ShowRoomCode(string roomCode)
        {
            _roomCodeInput.SetTextWithoutNotify(roomCode ?? string.Empty);
        }

        private void OnPlayWithBots() => PlayWithBotsRequested?.Invoke();

        private void OnCreateRoom() => CreateRoomRequested?.Invoke();

        private void OnQuickPlay() => QuickPlayRequested?.Invoke();

        private void OnSettings() => SettingsRequested?.Invoke();

        private void OnJoinRoom() => JoinRoomRequested?.Invoke(_roomCodeInput.text);

        private void OnRoomCodeSubmitted(string roomCode) => JoinRoomRequested?.Invoke(roomCode);
    }
}
