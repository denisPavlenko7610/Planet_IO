using System;
using Planet_IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Menu
{
    public interface INetworkMenuView
    {
        event Action<RoomConnectionSettings> HostRequested;
        event Action<RoomConnectionSettings> JoinRequested;
        event Action SinglePlayerRequested;

        void SetInteractionEnabled(bool interactionEnabled);
        void ShowStatus(string status, bool isError);
    }

    public sealed class NetworkUI : MonoBehaviour, INetworkMenuView
    {
        private const string AddressPreference = "PlanetIO.Room.Address";
        private const string PortPreference = "PlanetIO.Room.Port";
        private const string RoomPreference = "PlanetIO.Room.Code";

        [Header("Existing menu")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        [Header("Room controls (created automatically when empty)")]
        [SerializeField] private TMP_InputField _roomCodeInput;
        [SerializeField] private TMP_InputField _addressInput;
        [SerializeField] private TMP_InputField _portInput;
        [SerializeField] private TMP_InputField _maxPlayersInput;
        [SerializeField] private Button _singlePlayerButton;
        [SerializeField] private TMP_Text _statusText;

        public event Action<RoomConnectionSettings> HostRequested;
        public event Action<RoomConnectionSettings> JoinRequested;
        public event Action SinglePlayerRequested;

        private void Awake()
        {
            EnsureRoomControls();
            LoadPreferences();
        }

        private void OnEnable()
        {
            EnsureRoomControls();
            _hostButton?.onClick.AddListener(OnHostRequested);
            _clientButton?.onClick.AddListener(OnJoinRequested);
            _singlePlayerButton?.onClick.AddListener(
                OnSinglePlayerRequested);
        }

        private void OnDisable()
        {
            _hostButton?.onClick.RemoveListener(OnHostRequested);
            _clientButton?.onClick.RemoveListener(OnJoinRequested);
            _singlePlayerButton?.onClick.RemoveListener(
                OnSinglePlayerRequested);
        }

        public void SetInteractionEnabled(bool interactionEnabled)
        {
            SetInteractable(_hostButton, interactionEnabled);
            SetInteractable(_clientButton, interactionEnabled);
            SetInteractable(_singlePlayerButton, interactionEnabled);
            SetInteractable(_roomCodeInput, interactionEnabled);
            SetInteractable(_addressInput, interactionEnabled);
            SetInteractable(_portInput, interactionEnabled);
            SetInteractable(_maxPlayersInput, interactionEnabled);
        }

        public void ShowStatus(string status, bool isError)
        {
            if (_statusText == null)
            {
                return;
            }

            _statusText.text = status ?? string.Empty;
            _statusText.color = isError
                ? new Color(1f, 0.38f, 0.38f)
                : new Color(0.75f, 0.9f, 1f);
        }

        private void OnHostRequested()
        {
            if (!TryReadSettings(out RoomConnectionSettings settings))
            {
                return;
            }

            SavePreferences(settings);
            HostRequested?.Invoke(settings);
        }

        private void OnJoinRequested()
        {
            if (!TryReadSettings(out RoomConnectionSettings settings))
            {
                return;
            }

            SavePreferences(settings);
            JoinRequested?.Invoke(settings);
        }

        private void OnSinglePlayerRequested()
        {
            SinglePlayerRequested?.Invoke();
        }

        private bool TryReadSettings(out RoomConnectionSettings settings)
        {
            settings = default;
            string roomCode = RoomRules.NormalizeRoomCode(
                _roomCodeInput?.text);

            if (!RoomRules.IsValidRoomCode(roomCode))
            {
                ShowStatus(
                    $"Код комнаты должен содержать " +
                    $"{RoomRules.MinimumRoomCodeLength}–" +
                    $"{RoomRules.MaximumRoomCodeLength} букв или цифр.",
                    true);
                _roomCodeInput?.Select();
                return false;
            }

            if (!RoomRules.TryParsePort(_portInput?.text, out ushort port))
            {
                ShowStatus("Порт должен быть числом от 1 до 65535.", true);
                _portInput?.Select();
                return false;
            }

            int maxPlayers = RoomRules.DefaultMaxPlayers;
            if (!int.TryParse(_maxPlayersInput?.text, out maxPlayers))
            {
                maxPlayers = RoomRules.DefaultMaxPlayers;
            }

            settings = new RoomConnectionSettings(
                roomCode,
                _addressInput?.text,
                port,
                maxPlayers);
            return true;
        }

        private void EnsureRoomControls()
        {
            if (_hostButton == null || _clientButton == null)
            {
                return;
            }

            Transform controlsRoot = _hostButton.transform.parent;
            TMP_InputField inputTemplate =
                controlsRoot.GetComponentInChildren<TMP_InputField>(true);
            if (inputTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(NetworkUI)} needs a TMP input template.",
                    this);
                return;
            }

            if (controlsRoot.TryGetComponent(out VerticalLayoutGroup layout))
            {
                layout.spacing = 9f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            int insertionIndex = _hostButton.transform.GetSiblingIndex();
            _roomCodeInput ??= CreateInput(
                inputTemplate,
                controlsRoot,
                "RoomCodeInput",
                "Код комнаты (например PLANET)",
                CreateRoomCode(),
                TMP_InputField.ContentType.Alphanumeric,
                RoomRules.MaximumRoomCodeLength,
                insertionIndex++);
            _addressInput ??= CreateInput(
                inputTemplate,
                controlsRoot,
                "ServerAddressInput",
                "Адрес хоста / IP",
                RoomRules.DefaultAddress,
                TMP_InputField.ContentType.Standard,
                128,
                insertionIndex++);
            _portInput ??= CreateInput(
                inputTemplate,
                controlsRoot,
                "ServerPortInput",
                "Порт",
                RoomRules.DefaultPort.ToString(),
                TMP_InputField.ContentType.IntegerNumber,
                5,
                insertionIndex++);
            _maxPlayersInput ??= CreateInput(
                inputTemplate,
                controlsRoot,
                "MaxPlayersInput",
                "Игроков в комнате (1–16)",
                RoomRules.DefaultMaxPlayers.ToString(),
                TMP_InputField.ContentType.IntegerNumber,
                2,
                insertionIndex);

            SetButtonLabel(_hostButton, "Создать комнату");
            SetButtonLabel(_clientButton, "Войти в комнату");

            if (_singlePlayerButton == null)
            {
                _singlePlayerButton = Instantiate(
                    _hostButton,
                    controlsRoot);
                _singlePlayerButton.name = "SinglePlayerButton";
                _singlePlayerButton.onClick =
                    new Button.ButtonClickedEvent();
                SetButtonLabel(_singlePlayerButton, "Играть одному");
            }

            if (_statusText == null)
            {
                TMP_Text labelTemplate =
                    _hostButton.GetComponentInChildren<TMP_Text>(true);
                _statusText = Instantiate(labelTemplate, controlsRoot);
                _statusText.name = "ConnectionStatus";
                _statusText.text = "Введите код комнаты и адрес хоста";
                _statusText.alignment = TextAlignmentOptions.Center;
                _statusText.textWrappingMode = TextWrappingModes.Normal;
                _statusText.raycastTarget = false;
                _statusText.fontSize = 22f;
                _statusText.overflowMode = TextOverflowModes.Ellipsis;
                _statusText.color = new Color(0.75f, 0.9f, 1f);
                _statusText.gameObject.SetActive(true);
                _statusText.rectTransform.localScale = Vector3.one;
                _statusText.rectTransform.sizeDelta =
                    new Vector2(500f, 44f);
            }

            ApplyCompactLayout(controlsRoot);
        }

        private static TMP_InputField CreateInput(
            TMP_InputField template,
            Transform parent,
            string objectName,
            string placeholder,
            string initialValue,
            TMP_InputField.ContentType contentType,
            int characterLimit,
            int siblingIndex)
        {
            TMP_InputField input = Instantiate(template, parent);
            input.name = objectName;
            input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.onValueChanged = new TMP_InputField.OnChangeEvent();
            input.contentType = contentType;
            input.characterLimit = characterLimit;
            input.SetTextWithoutNotify(initialValue);
            input.transform.SetSiblingIndex(siblingIndex);
            input.textComponent.fontSize = 28f;
            input.textComponent.enableAutoSizing = true;
            input.textComponent.fontSizeMin = 18f;
            input.textComponent.fontSizeMax = 28f;

            if (input.placeholder is TMP_Text placeholderText)
            {
                placeholderText.text = placeholder;
                placeholderText.fontSize = 20f;
                placeholderText.enableAutoSizing = true;
                placeholderText.fontSizeMin = 14f;
                placeholderText.fontSizeMax = 20f;
            }

            return input;
        }

        private void LoadPreferences()
        {
            SetText(
                _addressInput,
                PlayerPrefs.GetString(
                    AddressPreference,
                    RoomRules.DefaultAddress));
            SetText(
                _portInput,
                PlayerPrefs.GetInt(
                    PortPreference,
                    RoomRules.DefaultPort).ToString());

            string savedRoom = PlayerPrefs.GetString(
                RoomPreference,
                _roomCodeInput?.text ?? CreateRoomCode());
            SetText(_roomCodeInput, savedRoom);
        }

        private static void SavePreferences(RoomConnectionSettings settings)
        {
            PlayerPrefs.SetString(AddressPreference, settings.Address);
            PlayerPrefs.SetInt(PortPreference, settings.Port);
            PlayerPrefs.SetString(RoomPreference, settings.RoomCode);
            PlayerPrefs.Save();
        }

        private static string CreateRoomCode() =>
            Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        private static void SetText(
            TMP_InputField inputField,
            string value)
        {
            inputField?.SetTextWithoutNotify(value);
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

        private static void SetButtonLabel(Button button, string text)
        {
            TMP_Text label = button?.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = text;
                label.enableAutoSizing = true;
                label.fontSizeMin = 16f;
                label.fontSizeMax = 28f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        private void ApplyCompactLayout(Transform controlsRoot)
        {
            foreach (Transform child in controlsRoot)
            {
                LayoutElement layoutElement =
                    child.GetComponent<LayoutElement>() ??
                    child.gameObject.AddComponent<LayoutElement>();
                layoutElement.flexibleHeight = 0f;

                if (child.TryGetComponent(out TMP_InputField input))
                {
                    layoutElement.minHeight = 48f;
                    layoutElement.preferredHeight = 54f;
                    input.textComponent.enableAutoSizing = true;
                    input.textComponent.fontSizeMin = 18f;
                    input.textComponent.fontSizeMax = 36f;

                    if (input.placeholder is TMP_Text placeholder)
                    {
                        placeholder.enableAutoSizing = true;
                        placeholder.fontSizeMin = 14f;
                        placeholder.fontSizeMax = 20f;
                    }
                }
                else if (child.TryGetComponent(out Button button))
                {
                    layoutElement.minHeight = 52f;
                    layoutElement.preferredHeight = 60f;

                    if (button == _hostButton ||
                        button == _clientButton ||
                        button == _singlePlayerButton)
                    {
                        TMP_Text label =
                            button.GetComponentInChildren<TMP_Text>(true);
                        if (label != null)
                        {
                            label.fontSizeMax = 28f;
                        }
                    }
                }
                else if (child.TryGetComponent(out TMP_Text text))
                {
                    layoutElement.minHeight = 32f;
                    layoutElement.preferredHeight =
                        text == _statusText ? 44f : 38f;
                }
            }
        }
    }
}
