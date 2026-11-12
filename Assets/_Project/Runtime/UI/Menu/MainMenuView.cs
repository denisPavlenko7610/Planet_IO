using System;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

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

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        private const string ErrorClass = "status--error";
        private const string PortraitClass = "menu--portrait";

        private VisualElement _menuRoot;

        private ILocalizationService _localization;
        private LocalizedLabels _labels;
        private Button _playWithBotsButton;
        private Button _createRoomButton;
        private Button _quickPlayButton;
        private Button _joinRoomButton;
        private Button _settingsButton;
        private TextField _roomCodeInput;
        private Label _statusText;
        private Label _bestScoreText;

        public event Action PlayWithBotsRequested;
        public event Action CreateRoomRequested;
        public event Action QuickPlayRequested;
        public event Action<string> JoinRoomRequested;
        public event Action SettingsRequested;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _playWithBotsButton = root.Q<Button>("play-with-bots");
            _createRoomButton = root.Q<Button>("create-room");
            _quickPlayButton = root.Q<Button>("quick-play");
            _joinRoomButton = root.Q<Button>("join-room");
            _settingsButton = root.Q<Button>("settings-button");
            _roomCodeInput = root.Q<TextField>("room-code-input");
            _statusText = root.Q<Label>("status-text");
            _bestScoreText = root.Q<Label>("best-score");
            _menuRoot = root.Q<VisualElement>("menu-root");
            _menuRoot.RegisterCallback<GeometryChangedEvent>(OnMenuGeometryChanged);

            _roomCodeInput.maxLength = RoomRules.MaximumRoomCodeLength;
            _roomCodeInput.RegisterValueChangedCallback(OnRoomCodeEdited);
            _roomCodeInput.RegisterCallback<KeyDownEvent>(OnRoomCodeKeyDown);

            _playWithBotsButton.clicked += OnPlayWithBots;
            _createRoomButton.clicked += OnCreateRoom;
            _quickPlayButton.clicked += OnQuickPlay;
            _joinRoomButton.clicked += OnJoinRoom;
            _settingsButton.clicked += OnSettings;

            _labels?.Dispose();
            _labels = new LocalizedLabels(_localization);
            _labels.BindLanguageClass(root);
            _labels.Bind(_playWithBotsButton, LocalizationKeys.MenuPlayWithBots);
            _labels.Bind(_createRoomButton, LocalizationKeys.MenuCreateRoom);
            _labels.Bind(_quickPlayButton, LocalizationKeys.MenuQuickPlay);
            _labels.Bind(_joinRoomButton, LocalizationKeys.MenuJoinRoom);
            _labels.Bind(_settingsButton, LocalizationKeys.MenuSettings);
            _labels.Bind(root.Q<Button>("random-nickname"), LocalizationKeys.MenuRandomNickname);
            _labels.Bind(root.Q<Label>("color-label"), LocalizationKeys.MenuColor);
            _labels.Bind(root.Q<Label>("planet-label"), LocalizationKeys.MenuPlanet);
            _labels.Bind(root.Q<Label>("online-label"), LocalizationKeys.MenuOnline);
            _labels.BindPlaceholder(_roomCodeInput, LocalizationKeys.MenuRoomCodePlaceholder);
        }

        private void OnDisable()
        {
            _playWithBotsButton.clicked -= OnPlayWithBots;
            _createRoomButton.clicked -= OnCreateRoom;
            _quickPlayButton.clicked -= OnQuickPlay;
            _joinRoomButton.clicked -= OnJoinRoom;
            _settingsButton.clicked -= OnSettings;
            _roomCodeInput.UnregisterValueChangedCallback(OnRoomCodeEdited);
            _roomCodeInput.UnregisterCallback<KeyDownEvent>(OnRoomCodeKeyDown);
            _menuRoot.UnregisterCallback<GeometryChangedEvent>(OnMenuGeometryChanged);
            _labels?.Dispose();
            _labels = null;
        }

        public void SetInteractionEnabled(bool interactionEnabled)
        {
            _playWithBotsButton.SetEnabled(interactionEnabled);
            _createRoomButton.SetEnabled(interactionEnabled);
            _quickPlayButton.SetEnabled(interactionEnabled);
            _joinRoomButton.SetEnabled(interactionEnabled);
            _roomCodeInput.SetEnabled(interactionEnabled);
        }

        public void ShowStatus(string status, bool isError)
        {
            _statusText.text = status ?? string.Empty;
            _statusText.EnableInClassList(ErrorClass, isError);
        }

        public void ShowBestScore(string text)
        {
            _bestScoreText.text = text ?? string.Empty;
            _bestScoreText.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void ShowRoomCode(string roomCode)
        {
            _roomCodeInput.SetValueWithoutNotify(roomCode ?? string.Empty);
        }

        private void OnMenuGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            Rect layout = _menuRoot.layout;
            _menuRoot.EnableInClassList(PortraitClass, layout.height > layout.width);
        }

        private void OnRoomCodeEdited(ChangeEvent<string> changeEvent)
        {
            string filtered = Filter(changeEvent.newValue);
            if (filtered != changeEvent.newValue)
            {
                _roomCodeInput.SetValueWithoutNotify(filtered);
            }
        }

        private void OnRoomCodeKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode == KeyCode.Return || keyEvent.keyCode == KeyCode.KeypadEnter)
            {
                JoinRoomRequested?.Invoke(_roomCodeInput.value);
            }
        }

        private static string Filter(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new(value.Length);
            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character) && character < 128)
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString();
        }

        private void OnPlayWithBots() => PlayWithBotsRequested?.Invoke();

        private void OnCreateRoom() => CreateRoomRequested?.Invoke();

        private void OnQuickPlay() => QuickPlayRequested?.Invoke();

        private void OnSettings() => SettingsRequested?.Invoke();

        private void OnJoinRoom() => JoinRoomRequested?.Invoke(_roomCodeInput.value);
    }
}
