using System;
using System.Collections.Generic;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PlanetIO.UI.Settings
{
    public interface ISettingsView
    {
        event Action<float> MusicVolumeChanged;
        event Action<float> SfxVolumeChanged;
        event Action<bool> HapticsChanged;
        event Action<bool> ControlsSwappedChanged;
        event Action<bool> FixedJoystickChanged;
        event Action<int> BotDifficultyChanged;
        event Action PreviousLanguageRequested;
        event Action NextLanguageRequested;
        event Action AdPrivacyRequested;
        event Action PrivacyPolicyRequested;
        event Action CloseRequested;

        bool IsVisible { get; }

        void Show();
        void Hide();
        void ShowValues(float music, float sfx, bool haptics, bool controlsSwapped);
        void ShowFixedJoystick(bool fixedJoystick);
        void ShowBotDifficulty(int difficulty);
        void SetDifficultyEditable(bool editable);
        void ShowLanguage(string languageName);
        void SetAdPrivacyVisible(bool visible);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        private const string FillClass = "slider-fill";
        private const string KnobClass = "toggle-knob";
        private const string RowClass = "settings-row";

        private readonly List<(VisualElement Row, EventCallback<ClickEvent> Callback)> _rowToggles = new();

        private ILocalizationService _localization;
        private LocalizedLabels _labels;
        private VisualElement _overlay;
        private VisualElement _backdrop;
        private Slider _musicSlider;
        private Slider _sfxSlider;
        private VisualElement _musicFill;
        private VisualElement _sfxFill;
        private Toggle _hapticsToggle;
        private Toggle _controlsSwappedToggle;
        private Toggle _fixedJoystickToggle;
        private Button _previousDifficultyButton;
        private Button _nextDifficultyButton;
        private Label _difficultyText;
        private int _difficulty;
        private Button _previousLanguageButton;
        private Button _nextLanguageButton;
        private Label _languageText;
        private Button _adPrivacyButton;
        private Button _privacyPolicyButton;
        private Button _closeButton;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<bool> HapticsChanged;
        public event Action<bool> ControlsSwappedChanged;
        public event Action<bool> FixedJoystickChanged;
        public event Action<int> BotDifficultyChanged;
        public event Action PreviousLanguageRequested;
        public event Action NextLanguageRequested;
        public event Action AdPrivacyRequested;
        public event Action PrivacyPolicyRequested;
        public event Action CloseRequested;

        public bool IsVisible => _overlay != null && _overlay.resolvedStyle.display == DisplayStyle.Flex;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _overlay = root.Q<VisualElement>("settings-overlay");
            _backdrop = root.Q<VisualElement>("settings-backdrop");
            _musicSlider = root.Q<Slider>("music-slider");
            _sfxSlider = root.Q<Slider>("sound-slider");
            _hapticsToggle = root.Q<Toggle>("haptics-toggle");
            _controlsSwappedToggle = root.Q<Toggle>("controls-swapped-toggle");
            _fixedJoystickToggle = root.Q<Toggle>("fixed-joystick-toggle");
            _previousDifficultyButton = root.Q<Button>("previous-difficulty");
            _nextDifficultyButton = root.Q<Button>("next-difficulty");
            _difficultyText = root.Q<Label>("difficulty-value");
            _previousLanguageButton = root.Q<Button>("previous-language");
            _nextLanguageButton = root.Q<Button>("next-language");
            _languageText = root.Q<Label>("language-value");
            _adPrivacyButton = root.Q<Button>("ad-privacy");
            _privacyPolicyButton = root.Q<Button>("privacy-policy");
            _closeButton = root.Q<Button>("settings-close");

            _musicFill = CreateFill(_musicSlider);
            _sfxFill = CreateFill(_sfxSlider);
            AddToggleKnob(_hapticsToggle);
            AddToggleKnob(_controlsSwappedToggle);
            AddToggleKnob(_fixedJoystickToggle);
            BindRowToggle(_hapticsToggle);
            BindRowToggle(_controlsSwappedToggle);
            BindRowToggle(_fixedJoystickToggle);
            Hide();

            _musicSlider.RegisterValueChangedCallback(OnMusicChanged);
            _sfxSlider.RegisterValueChangedCallback(OnSfxChanged);
            _hapticsToggle.RegisterValueChangedCallback(OnHapticsChanged);
            _controlsSwappedToggle.RegisterValueChangedCallback(OnControlsSwappedChanged);
            _fixedJoystickToggle.RegisterValueChangedCallback(OnFixedJoystickChanged);
            _previousDifficultyButton.clicked += OnPreviousDifficulty;
            _nextDifficultyButton.clicked += OnNextDifficulty;
            _previousLanguageButton.clicked += OnPreviousLanguage;
            _nextLanguageButton.clicked += OnNextLanguage;
            _adPrivacyButton.clicked += OnAdPrivacy;
            _privacyPolicyButton.clicked += OnPrivacyPolicy;
            _closeButton.clicked += OnClose;
            _backdrop.RegisterCallback<ClickEvent>(OnBackdropClicked);

            _labels?.Dispose();
            _labels = new LocalizedLabels(_localization);
            _labels.Bind(root.Q<Label>("settings-title"), LocalizationKeys.SettingsTitle);
            _labels.Bind(root.Q<Label>("music-label"), LocalizationKeys.SettingsMusic);
            _labels.Bind(root.Q<Label>("sound-label"), LocalizationKeys.SettingsSound);
            _labels.Bind(root.Q<Label>("haptics-label"), LocalizationKeys.SettingsHaptics);
            _labels.Bind(root.Q<Label>("controls-swapped-label"), LocalizationKeys.SettingsControlsSwapped);
            _labels.Bind(root.Q<Label>("fixed-joystick-label"), LocalizationKeys.SettingsFixedJoystick);
            _labels.Bind(root.Q<Label>("difficulty-label"), LocalizationKeys.SettingsBotDifficulty);
            _labels.Bind(root.Q<Label>("difficulty-hint"), LocalizationKeys.SettingsBotsOnly);
            _labels.Bind(root.Q<Label>("language-label"), LocalizationKeys.SettingsLanguage);
            _labels.Bind(_adPrivacyButton, LocalizationKeys.SettingsAdPrivacy);
            _labels.Bind(_privacyPolicyButton, LocalizationKeys.SettingsPrivacyPolicy);
            _labels.Bind(_closeButton, LocalizationKeys.SettingsClose);
        }

        private void OnDisable()
        {
            _musicSlider.UnregisterValueChangedCallback(OnMusicChanged);
            _sfxSlider.UnregisterValueChangedCallback(OnSfxChanged);
            _hapticsToggle.UnregisterValueChangedCallback(OnHapticsChanged);
            _controlsSwappedToggle.UnregisterValueChangedCallback(OnControlsSwappedChanged);
            _fixedJoystickToggle.UnregisterValueChangedCallback(OnFixedJoystickChanged);
            _previousDifficultyButton.clicked -= OnPreviousDifficulty;
            _nextDifficultyButton.clicked -= OnNextDifficulty;
            _previousLanguageButton.clicked -= OnPreviousLanguage;
            _nextLanguageButton.clicked -= OnNextLanguage;
            _adPrivacyButton.clicked -= OnAdPrivacy;
            _privacyPolicyButton.clicked -= OnPrivacyPolicy;
            _closeButton.clicked -= OnClose;
            _backdrop.UnregisterCallback<ClickEvent>(OnBackdropClicked);
            foreach ((VisualElement row, EventCallback<ClickEvent> callback) in _rowToggles)
            {
                row.UnregisterCallback(callback);
            }

            _rowToggles.Clear();
            _labels?.Dispose();
            _labels = null;
        }

        public void Show() => _overlay.style.display = DisplayStyle.Flex;

        public void Hide() => _overlay.style.display = DisplayStyle.None;

        public void ShowValues(float music, float sfx, bool haptics, bool controlsSwapped)
        {
            _musicSlider.SetValueWithoutNotify(music);
            _sfxSlider.SetValueWithoutNotify(sfx);
            _hapticsToggle.SetValueWithoutNotify(haptics);
            _controlsSwappedToggle.SetValueWithoutNotify(controlsSwapped);
            UpdateFill(_musicFill, _musicSlider);
            UpdateFill(_sfxFill, _sfxSlider);
        }

        public void ShowFixedJoystick(bool fixedJoystick) => _fixedJoystickToggle.SetValueWithoutNotify(fixedJoystick);

        public void SetDifficultyEditable(bool editable)
        {
            _previousDifficultyButton.SetEnabled(editable);
            _nextDifficultyButton.SetEnabled(editable);
        }

        public void ShowBotDifficulty(int difficulty)
        {
            _difficulty = BotDifficultyRules.Normalize(difficulty);
            string key = _difficulty switch
            {
                0 => LocalizationKeys.DifficultyEasy,
                2 => LocalizationKeys.DifficultyHard,
                _ => LocalizationKeys.DifficultyMedium
            };
            _difficultyText.text = _localization.Get(key);
        }

        private void OnPreviousDifficulty() => BotDifficultyChanged?.Invoke((_difficulty + 2) % 3);
        private void OnNextDifficulty() => BotDifficultyChanged?.Invoke((_difficulty + 1) % 3);

        public void ShowLanguage(string languageName)
        {
            _languageText.text = languageName;
        }

        public void SetAdPrivacyVisible(bool visible)
        {
            _adPrivacyButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement CreateFill(Slider slider)
        {
            VisualElement tracker = slider.Q<VisualElement>(className: "unity-base-slider__tracker");
            VisualElement fill = new();
            fill.AddToClassList(FillClass);
            fill.pickingMode = PickingMode.Ignore;
            tracker.Add(fill);
            return fill;
        }

        private void BindRowToggle(Toggle toggle)
        {
            VisualElement row = toggle.parent;
            while (row != null && !row.ClassListContains(RowClass))
            {
                row = row.parent;
            }

            if (row == null)
            {
                return;
            }

            EventCallback<ClickEvent> callback = clickEvent =>
            {
                if (clickEvent.target is VisualElement target && (target == toggle || toggle.Contains(target)))
                {
                    return;
                }

                toggle.value = !toggle.value;
            };

            row.RegisterCallback(callback);
            _rowToggles.Add((row, callback));
        }

        private static void AddToggleKnob(Toggle toggle)
        {
            VisualElement checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (checkmark == null || checkmark.Q<VisualElement>(className: KnobClass) != null)
            {
                return;
            }

            VisualElement knob = new();
            knob.AddToClassList(KnobClass);
            knob.pickingMode = PickingMode.Ignore;
            checkmark.Add(knob);
        }

        private static void UpdateFill(VisualElement fill, Slider slider)
        {
            float range = Mathf.Max(0.0001f, slider.highValue - slider.lowValue);
            fill.style.width = Length.Percent(Mathf.Clamp01((slider.value - slider.lowValue) / range) * 100f);
        }

        private void OnMusicChanged(ChangeEvent<float> changeEvent)
        {
            UpdateFill(_musicFill, _musicSlider);
            MusicVolumeChanged?.Invoke(changeEvent.newValue);
        }

        private void OnSfxChanged(ChangeEvent<float> changeEvent)
        {
            UpdateFill(_sfxFill, _sfxSlider);
            SfxVolumeChanged?.Invoke(changeEvent.newValue);
        }

        private void OnHapticsChanged(ChangeEvent<bool> changeEvent) => HapticsChanged?.Invoke(changeEvent.newValue);

        private void OnControlsSwappedChanged(ChangeEvent<bool> changeEvent) => ControlsSwappedChanged?.Invoke(changeEvent.newValue);

        private void OnFixedJoystickChanged(ChangeEvent<bool> changeEvent) => FixedJoystickChanged?.Invoke(changeEvent.newValue);

        private void OnPreviousLanguage() => PreviousLanguageRequested?.Invoke();

        private void OnNextLanguage() => NextLanguageRequested?.Invoke();

        private void OnAdPrivacy() => AdPrivacyRequested?.Invoke();

        private void OnPrivacyPolicy() => PrivacyPolicyRequested?.Invoke();

        private void OnClose() => CloseRequested?.Invoke();

        private void OnBackdropClicked(ClickEvent clickEvent) => CloseRequested?.Invoke();
    }
}
