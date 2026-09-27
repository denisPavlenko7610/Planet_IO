using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Settings
{
    public interface ISettingsView
    {
        event Action<float> MusicVolumeChanged;
        event Action<float> SfxVolumeChanged;
        event Action<bool> HapticsChanged;
        event Action PreviousLanguageRequested;
        event Action NextLanguageRequested;
        event Action AdPrivacyRequested;
        event Action CloseRequested;

        bool IsVisible { get; }

        void Show();
        void Hide();
        void ShowValues(float music, float sfx, bool haptics);
        void ShowLanguage(string languageName);
        void SetAdPrivacyVisible(bool visible);
    }

    public sealed class SettingsView : MonoBehaviour, ISettingsView
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Button _previousLanguageButton;
        [SerializeField] private Button _nextLanguageButton;
        [SerializeField] private TMP_Text _languageText;
        [SerializeField] private Button _adPrivacyButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _backdropButton;

        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;
        public event Action<bool> HapticsChanged;
        public event Action PreviousLanguageRequested;
        public event Action NextLanguageRequested;
        public event Action AdPrivacyRequested;
        public event Action CloseRequested;

        public bool IsVisible => _root.activeSelf;

        private void Awake()
        {
            _root.SetActive(false);
        }

        private void OnEnable()
        {
            _musicSlider.onValueChanged.AddListener(OnMusicChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            _hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
            _previousLanguageButton.onClick.AddListener(OnPreviousLanguage);
            _nextLanguageButton.onClick.AddListener(OnNextLanguage);
            _adPrivacyButton.onClick.AddListener(OnAdPrivacy);
            _closeButton.onClick.AddListener(OnClose);
            _backdropButton.onClick.AddListener(OnClose);
        }

        private void OnDisable()
        {
            _musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            _hapticsToggle.onValueChanged.RemoveListener(OnHapticsChanged);
            _previousLanguageButton.onClick.RemoveListener(OnPreviousLanguage);
            _nextLanguageButton.onClick.RemoveListener(OnNextLanguage);
            _adPrivacyButton.onClick.RemoveListener(OnAdPrivacy);
            _closeButton.onClick.RemoveListener(OnClose);
            _backdropButton.onClick.RemoveListener(OnClose);
        }

        public void Show() => _root.SetActive(true);

        public void Hide() => _root.SetActive(false);

        public void ShowValues(float music, float sfx, bool haptics)
        {
            _musicSlider.SetValueWithoutNotify(music);
            _sfxSlider.SetValueWithoutNotify(sfx);
            _hapticsToggle.SetIsOnWithoutNotify(haptics);
        }

        public void ShowLanguage(string languageName)
        {
            _languageText.text = languageName;
        }

        public void SetAdPrivacyVisible(bool visible)
        {
            _adPrivacyButton.gameObject.SetActive(visible);
        }

        private void OnMusicChanged(float value) => MusicVolumeChanged?.Invoke(value);

        private void OnSfxChanged(float value) => SfxVolumeChanged?.Invoke(value);

        private void OnHapticsChanged(bool enabled) => HapticsChanged?.Invoke(enabled);

        private void OnPreviousLanguage() => PreviousLanguageRequested?.Invoke();

        private void OnNextLanguage() => NextLanguageRequested?.Invoke();

        private void OnAdPrivacy() => AdPrivacyRequested?.Invoke();

        private void OnClose() => CloseRequested?.Invoke();
    }
}
