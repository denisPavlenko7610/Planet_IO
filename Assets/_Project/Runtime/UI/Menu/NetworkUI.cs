using System;
using PlanetIO;
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

        [Header("Existing menu")]
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        [Header("Room controls (created automatically when empty)")]
        [SerializeField] private TMP_InputField _joinCodeInput;
        [SerializeField] private Button _singlePlayerButton;
        [SerializeField] private TMP_Text _statusText;

        public event Action HostRequested;
        public event Action<string> JoinRequested;
        public event Action SinglePlayerRequested;

        private void Awake()
        {
            EnsureRoomControls();
            SafeAreaFitter.AttachTo(transform.parent);
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

            ConfigureLayout(controlsRoot);

            int insertionIndex = _hostButton.transform.GetSiblingIndex();
            _joinCodeInput ??= CreateInput(
                inputTemplate,
                controlsRoot,
                "JoinCodeInput",
                "Room code (from a friend)",
                string.Empty,
                TMP_InputField.ContentType.Alphanumeric,
                RoomRules.MaximumRoomCodeLength,
                insertionIndex);

            SetButtonLabels();
            EnsureSinglePlayerButton(controlsRoot);
            EnsureStatusText(controlsRoot);
            ApplyCompactLayout(controlsRoot);
        }

        private void SetButtonLabels()
        {
            SetButtonLabel(_hostButton, "Create Room");
            SetButtonLabel(_clientButton, "Join");
        }

        private void EnsureSinglePlayerButton(Transform controlsRoot)
        {
            if (_singlePlayerButton != null)
            {
                return;
            }

            _singlePlayerButton = Instantiate(_hostButton, controlsRoot);
            _singlePlayerButton.name = "SinglePlayerButton";
            _singlePlayerButton.onClick = new Button.ButtonClickedEvent();
            SetButtonLabel(_singlePlayerButton, "Single Player");
        }

        private void EnsureStatusText(Transform controlsRoot)
        {
            if (_statusText != null)
            {
                return;
            }

            TMP_Text labelTemplate =
                _hostButton.GetComponentInChildren<TMP_Text>(true);
            _statusText = Instantiate(labelTemplate, controlsRoot);
            _statusText.name = "ConnectionStatus";
            _statusText.text = "Click \"Create Room\" or enter a code";
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.textWrappingMode = TextWrappingModes.Normal;
            _statusText.raycastTarget = false;
            _statusText.fontSize = 22f;
            _statusText.overflowMode = TextOverflowModes.Ellipsis;
            _statusText.color = InfoColor;
            _statusText.gameObject.SetActive(true);
            _statusText.rectTransform.localScale = Vector3.one;
            _statusText.rectTransform.sizeDelta = new Vector2(500f, 44f);
        }

        private static void ConfigureLayout(Transform controlsRoot)
        {
            if (controlsRoot.TryGetComponent(out VerticalLayoutGroup layout))
            {
                layout.spacing = 9f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }
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

        private void ShowRoomSettings()
        {
            SetText(_joinCodeInput, string.Empty);
        }

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
