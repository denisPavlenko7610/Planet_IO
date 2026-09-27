using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Hud
{
    public interface ISessionHudView
    {
        event Action LeaveRequested;
        event Action PlayAgainRequested;
        event Action ContinueRequested;

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

    public sealed class SessionHudView : MonoBehaviour, ISessionHudView
    {
        private const float KillFeedFadeSeconds = 3f;
        private const float ScorePopupSeconds = 0.9f;
        private const float ScorePopupRiseSpeed = 80f;
        private const float HintFadeSeconds = 4f;

        [Header("Corner info")]
        [SerializeField] private TMP_Text _sessionText;
        [SerializeField] private TMP_Text _leaderboardText;
        [SerializeField] private Button _leaveButton;

        [Header("Defeat")]
        [SerializeField] private GameObject _defeatPanel;
        [SerializeField] private TMP_Text _defeatTitleText;
        [SerializeField] private TMP_Text _defeatBodyText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _defeatLeaveButton;

        [Header("Feedback")]
        [SerializeField] private TMP_Text _killFeedText;
        [SerializeField] private TMP_Text _scorePopupText;
        [SerializeField] private TMP_Text _hintText;
        [SerializeField] private Image _borderWarning;

        public event Action LeaveRequested;
        public event Action PlayAgainRequested;
        public event Action ContinueRequested;

        public bool IsDefeatVisible => _defeatPanel.activeSelf;

        private float _killFeedTimeRemaining;
        private float _scorePopupTimeRemaining;
        private float _hintTimeRemaining;

        private void Awake()
        {
            _defeatPanel.SetActive(false);
            _killFeedText.alpha = 0f;
            _scorePopupText.alpha = 0f;
            _hintText.alpha = 0f;
        }

        private void OnEnable()
        {
            _leaveButton.onClick.AddListener(OnLeaveClicked);
            _defeatLeaveButton.onClick.AddListener(OnLeaveClicked);
            _playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            _leaveButton.onClick.RemoveListener(OnLeaveClicked);
            _defeatLeaveButton.onClick.RemoveListener(OnLeaveClicked);
            _playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            _continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (_killFeedTimeRemaining > 0f)
            {
                _killFeedTimeRemaining -= deltaTime;
                _killFeedText.alpha = Mathf.Clamp01(_killFeedTimeRemaining / KillFeedFadeSeconds);
            }

            if (_scorePopupTimeRemaining > 0f)
            {
                _scorePopupTimeRemaining -= deltaTime;
                _scorePopupText.alpha = Mathf.Clamp01(_scorePopupTimeRemaining / ScorePopupSeconds);
                _scorePopupText.rectTransform.anchoredPosition += Vector2.up * (ScorePopupRiseSpeed * deltaTime);
            }

            if (_hintTimeRemaining > 0f)
            {
                _hintTimeRemaining -= deltaTime;
                _hintText.alpha = Mathf.Clamp01(_hintTimeRemaining / (HintFadeSeconds * 0.4f));
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
            _playAgainButton.interactable = true;
            _defeatPanel.SetActive(true);
        }

        public void HideDefeat()
        {
            _defeatPanel.SetActive(false);
        }

        public void SetButtonLabels(string leave, string playAgain, string watchAd)
        {
            SetButtonLabel(_leaveButton, leave);
            SetButtonLabel(_defeatLeaveButton, leave);
            SetButtonLabel(_playAgainButton, playAgain);
            SetButtonLabel(_continueButton, watchAd);
        }

        public void SetLeaveButtonInteractable(bool interactable)
        {
            _leaveButton.interactable = interactable;
            _defeatLeaveButton.interactable = interactable;
        }

        public void SetContinueVisible(bool visible)
        {
            _continueButton.gameObject.SetActive(visible);
        }

        public void ShowKillFeed(string message)
        {
            _killFeedText.text = message;
            _killFeedText.alpha = 1f;
            _killFeedTimeRemaining = KillFeedFadeSeconds;
        }

        public void ShowScorePopup(Vector2 screenPosition, int score)
        {
            RectTransform parentRect = (RectTransform)_scorePopupText.rectTransform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, null, out Vector2 localPoint))
            {
                _scorePopupText.rectTransform.anchoredPosition = localPoint;
            }

            _scorePopupText.text = $"+{score:N0}";
            _scorePopupText.alpha = 1f;
            _scorePopupTimeRemaining = ScorePopupSeconds;
        }

        public void ShowHint(string message)
        {
            _hintText.text = message;
            _hintText.alpha = 1f;
            _hintTimeRemaining = HintFadeSeconds;
        }

        public void SetBorderWarning(float strength)
        {
            Color color = _borderWarning.color;
            color.a = Mathf.Clamp01(strength);
            _borderWarning.color = color;
            _borderWarning.enabled = color.a > 0.001f;
        }

        private static void SetButtonLabel(Button button, string label)
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        private void OnLeaveClicked() => LeaveRequested?.Invoke();

        private void OnPlayAgainClicked() => PlayAgainRequested?.Invoke();

        private void OnContinueClicked() => ContinueRequested?.Invoke();
    }
}
