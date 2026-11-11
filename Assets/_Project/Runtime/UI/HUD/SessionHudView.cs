using System;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PlanetIO.UI.Hud
{
    public interface ISessionHudView
    {
        event Action LeaveRequested;
        event Action PlayAgainRequested;
        event Action ContinueRequested;
        event Action SettingsRequested;

        void ShowSessionText(string text);
        void ShowLeaderboardText(string text);
        void ShowDefeat(string title, string body);
        void HideDefeat();
        void SetButtonLabels(string leave, string playAgain, string watchAd);
        void SetLeaveButtonInteractable(bool interactable);
        void SetContinueVisible(bool visible);
        void ShowKillFeed(string message);
        void ShowScorePopup(Vector2 screenPosition, int score);
        void ShowHint(string message);
        void SetBorderWarning(float strength);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SessionHudView : MonoBehaviour, ISessionHudView
    {
        private const float KillFeedFadeSeconds = 3f;
        private const float ScorePopupSeconds = 0.9f;
        private const float ScorePopupRiseSpeed = 80f;
        private const float HintFadeSeconds = 4f;

        private ILocalizationService _localization;
        private LocalizedLabels _labels;
        private VisualElement _root;
        private VisualElement _defeatPanel;
        private VisualElement _borderWarning;
        private Label _sessionText;
        private Label _leaderboardText;
        private Label _defeatTitleText;
        private Label _defeatBodyText;
        private Label _killFeedText;
        private Label _scorePopupText;
        private Label _hintText;
        private Button _leaveButton;
        private Button _settingsButton;
        private Button _playAgainButton;
        private Button _continueButton;
        private Button _defeatLeaveButton;

        private float _killFeedTimeRemaining;
        private float _scorePopupTimeRemaining;
        private float _hintTimeRemaining;
        private Vector2 _scorePopupOrigin;
        private bool _defeatVisible;

        public event Action LeaveRequested;
        public event Action PlayAgainRequested;
        public event Action ContinueRequested;
        public event Action SettingsRequested;

        public bool IsDefeatVisible => _defeatVisible;

        [Inject]
        public void Construct(ILocalizationService localization)
        {
            _localization = localization;
        }

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _defeatPanel = _root.Q<VisualElement>("defeat-panel");
            _borderWarning = _root.Q<VisualElement>("border-warning");
            _sessionText = _root.Q<Label>("session-text");
            _leaderboardText = _root.Q<Label>("leaderboard-text");
            _defeatTitleText = _root.Q<Label>("defeat-title");
            _defeatBodyText = _root.Q<Label>("defeat-body");
            _killFeedText = _root.Q<Label>("kill-feed");
            _scorePopupText = _root.Q<Label>("score-popup");
            _hintText = _root.Q<Label>("hint-text");
            _leaveButton = _root.Q<Button>("leave-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _playAgainButton = _root.Q<Button>("play-again");
            _continueButton = _root.Q<Button>("continue-button");
            _defeatLeaveButton = _root.Q<Button>("defeat-leave");

            _defeatPanel.style.display = DisplayStyle.None;
            _defeatVisible = false;
            _killFeedText.style.opacity = 0f;
            _scorePopupText.style.opacity = 0f;
            _hintText.style.opacity = 0f;

            _leaveButton.clicked += OnLeaveClicked;
            _settingsButton.clicked += OnSettingsClicked;
            _defeatLeaveButton.clicked += OnLeaveClicked;
            _playAgainButton.clicked += OnPlayAgainClicked;
            _continueButton.clicked += OnContinueClicked;

            _labels?.Dispose();
            _labels = new LocalizedLabels(_localization);
            _labels.BindLanguageClass(_root);
            _labels.Bind(_settingsButton, LocalizationKeys.MenuSettings);
            _labels.Bind(_root.Q<Label>("loading-title"), LocalizationKeys.StatusLoading);
        }

        private void OnDisable()
        {
            _leaveButton.clicked -= OnLeaveClicked;
            _settingsButton.clicked -= OnSettingsClicked;
            _defeatLeaveButton.clicked -= OnLeaveClicked;
            _playAgainButton.clicked -= OnPlayAgainClicked;
            _continueButton.clicked -= OnContinueClicked;
            _labels?.Dispose();
            _labels = null;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (_killFeedTimeRemaining > 0f)
            {
                _killFeedTimeRemaining -= deltaTime;
                _killFeedText.style.opacity = Mathf.Clamp01(_killFeedTimeRemaining / KillFeedFadeSeconds);
            }

            if (_scorePopupTimeRemaining > 0f)
            {
                _scorePopupTimeRemaining -= deltaTime;
                _scorePopupOrigin.y -= ScorePopupRiseSpeed * deltaTime;
                _scorePopupText.style.top = _scorePopupOrigin.y;
                _scorePopupText.style.opacity = Mathf.Clamp01(_scorePopupTimeRemaining / ScorePopupSeconds);
            }

            if (_hintTimeRemaining > 0f)
            {
                _hintTimeRemaining -= deltaTime;
                _hintText.style.opacity = Mathf.Clamp01(_hintTimeRemaining / (HintFadeSeconds * 0.4f));
            }
        }

        public void ShowSessionText(string text)
        {
            _sessionText.text = text;
        }

        public void ShowLeaderboardText(string text)
        {
            _leaderboardText.text = text;
        }

        public void ShowDefeat(string title, string body)
        {
            _defeatTitleText.text = title;
            _defeatBodyText.text = body;
            _playAgainButton.SetEnabled(true);
            _defeatPanel.style.display = DisplayStyle.Flex;
            _defeatVisible = true;
        }

        public void HideDefeat()
        {
            _defeatPanel.style.display = DisplayStyle.None;
            _defeatVisible = false;
        }

        public void SetButtonLabels(string leave, string playAgain, string watchAd)
        {
            _leaveButton.text = leave;
            _defeatLeaveButton.text = leave;
            _playAgainButton.text = playAgain;
            _continueButton.text = watchAd;
        }

        public void SetLeaveButtonInteractable(bool interactable)
        {
            _leaveButton.SetEnabled(interactable);
            _defeatLeaveButton.SetEnabled(interactable);
        }

        public void SetContinueVisible(bool visible)
        {
            _continueButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowKillFeed(string message)
        {
            _killFeedText.text = message;
            _killFeedText.style.opacity = 1f;
            _killFeedTimeRemaining = KillFeedFadeSeconds;
        }

        public void ShowScorePopup(Vector2 screenPosition, int score)
        {
            IPanel panel = _root.panel;
            if (panel != null)
            {
                _scorePopupOrigin = RuntimePanelUtils.ScreenToPanel(
                    panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                _scorePopupText.style.left = _scorePopupOrigin.x;
                _scorePopupText.style.top = _scorePopupOrigin.y;
            }

            _scorePopupText.text = $"+{score:N0}";
            _scorePopupText.style.opacity = 1f;
            _scorePopupTimeRemaining = ScorePopupSeconds;
        }

        public void ShowHint(string message)
        {
            _hintText.text = message;
            _hintText.style.opacity = 1f;
            _hintTimeRemaining = HintFadeSeconds;
        }

        public void SetBorderWarning(float strength)
        {
            _borderWarning.style.opacity = Mathf.Clamp01(strength);
        }

        private void OnLeaveClicked() => LeaveRequested?.Invoke();

        private void OnSettingsClicked() => SettingsRequested?.Invoke();

        private void OnPlayAgainClicked() => PlayAgainRequested?.Invoke();

        private void OnContinueClicked() => ContinueRequested?.Invoke();
    }
}
