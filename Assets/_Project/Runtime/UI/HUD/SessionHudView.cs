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

        void ShowSessionText(string text);
        void ShowLeaderboardText(string text);
        void ShowDefeat(int finalScore, int bestScore, bool canPlayAgain);
        void SetLeaveButtonInteractable(bool interactable);
        void SetPlayAgainVisible(bool visible);
        void ShowKillFeed(string message);
        void ShowScorePopup(Vector2 screenPosition, int score);
    }

    public sealed class SessionHudView : MonoBehaviour, ISessionHudView
    {
        private const string DefeatTitle = "YOU LOST";
        private const float KillFeedFadeSeconds = 3f;
        private const float ScorePopupSeconds = 0.9f;
        private const float ScorePopupRiseSpeed = 80f;

        [SerializeField] private TMP_Text _sessionText;
        [SerializeField] private TMP_Text _leaderboardText;
        [SerializeField] private Button _leaveButton;

        public event Action LeaveRequested;
        public event Action PlayAgainRequested;

        public bool IsDefeatVisible =>
            _sessionText != null &&
            _sessionText.text == DefeatTitle;

        private Button _playAgainButton;
        private TMP_Text _killFeedText;
        private TMP_Text _scorePopupText;
        private float _killFeedTimeRemaining;
        private float _scorePopupTimeRemaining;

        private void Awake()
        {
            CreatePlayAgainButton();
            CreateKillFeedText();
            CreateScorePopupText();
        }

        private void OnEnable()
        {
            _leaveButton?.onClick.AddListener(OnLeaveClicked);
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }
        }

        private void OnDisable()
        {
            _leaveButton?.onClick.RemoveListener(OnLeaveClicked);
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            }
        }

        private void Update()
        {
            if (_killFeedTimeRemaining > 0f)
            {
                _killFeedTimeRemaining -= Time.unscaledDeltaTime;
                _killFeedText.alpha = Mathf.Clamp01(_killFeedTimeRemaining / KillFeedFadeSeconds);
            }

            if (_scorePopupTimeRemaining > 0f)
            {
                _scorePopupTimeRemaining -= Time.unscaledDeltaTime;
                float fade = Mathf.Clamp01(_scorePopupTimeRemaining / ScorePopupSeconds);
                _scorePopupText.alpha = fade;
                _scorePopupText.rectTransform.anchoredPosition +=
                    Vector2.up * (ScorePopupRiseSpeed * Time.unscaledDeltaTime);
            }
        }

        public void ShowSessionText(string text)
        {
            if (_sessionText != null)
            {
                _sessionText.text = text;
            }
        }

        public void ShowLeaderboardText(string text)
        {
            if (_leaderboardText != null)
            {
                _leaderboardText.text = text;
            }
        }

        public void ShowDefeat(int finalScore, int bestScore, bool canPlayAgain)
        {
            ShowSessionText(DefeatTitle);
            ShowLeaderboardText($"FINAL SCORE\n{finalScore:N0}\nBEST {bestScore:N0}");

            if (_playAgainButton != null)
            {
                _playAgainButton.gameObject.SetActive(canPlayAgain);
                _playAgainButton.interactable = true;
            }
        }

        public void SetLeaveButtonInteractable(bool interactable)
        {
            if (_leaveButton != null)
            {
                _leaveButton.interactable = interactable;
            }
        }

        public void SetPlayAgainVisible(bool visible)
        {
            if (_playAgainButton != null)
            {
                _playAgainButton.gameObject.SetActive(visible);
            }
        }

        public void ShowKillFeed(string message)
        {
            if (_killFeedText == null)
            {
                return;
            }

            _killFeedText.text = message;
            _killFeedText.alpha = 1f;
            _killFeedTimeRemaining = KillFeedFadeSeconds;
        }

        public void ShowScorePopup(Vector2 screenPosition, int score)
        {
            if (_scorePopupText == null)
            {
                return;
            }

            RectTransform canvasRect = _scorePopupText.transform.parent.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPosition, null, out Vector2 localPoint))
            {
                _scorePopupText.rectTransform.anchoredPosition = localPoint;
            }

            _scorePopupText.text = $"+{score:N0}";
            _scorePopupText.alpha = 1f;
            _scorePopupTimeRemaining = ScorePopupSeconds;
        }

        private void CreatePlayAgainButton()
        {
            if (_leaveButton == null)
            {
                return;
            }

            GameObject buttonObject = Instantiate(_leaveButton.gameObject, _leaveButton.transform.parent);
            buttonObject.name = "PlayAgainButton";
            _playAgainButton = buttonObject.GetComponent<Button>();

            RectTransform leaveRect = _leaveButton.GetComponent<RectTransform>();
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            if (leaveRect.parent.GetComponent<LayoutGroup>() == null)
            {
                rect.anchoredPosition = leaveRect.anchoredPosition + new Vector2(0f, -leaveRect.rect.height - 12f);
            }

            TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "PLAY AGAIN";
            }

            buttonObject.SetActive(false);
        }

        private void CreateKillFeedText()
        {
            Canvas canvas = _sessionText != null ? _sessionText.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                return;
            }

            GameObject textObject = new GameObject("KillFeedText", typeof(RectTransform));
            textObject.transform.SetParent(canvas.transform, false);

            _killFeedText = textObject.AddComponent<TextMeshProUGUI>();
            _killFeedText.font = _sessionText.font;
            _killFeedText.fontSize = _sessionText.fontSize * 0.7f;
            _killFeedText.alignment = TextAlignmentOptions.Center;
            _killFeedText.color = Color.white;
            _killFeedText.raycastTarget = false;
            _killFeedText.alpha = 0f;

            RectTransform rect = _killFeedText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(700f, 50f);
            rect.anchoredPosition = new Vector2(0f, -130f);
        }

        private void CreateScorePopupText()
        {
            Canvas canvas = _sessionText != null ? _sessionText.GetComponentInParent<Canvas>() : null;
            if (canvas == null)
            {
                return;
            }

            GameObject textObject = new GameObject("ScorePopupText", typeof(RectTransform));
            textObject.transform.SetParent(canvas.transform, false);

            _scorePopupText = textObject.AddComponent<TextMeshProUGUI>();
            _scorePopupText.font = _sessionText.font;
            _scorePopupText.fontSize = _sessionText.fontSize * 1.2f;
            _scorePopupText.alignment = TextAlignmentOptions.Center;
            _scorePopupText.color = new Color(0.6f, 1f, 0.5f);
            _scorePopupText.raycastTarget = false;
            _scorePopupText.alpha = 0f;

            RectTransform rect = _scorePopupText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300f, 60f);
        }

        private void OnLeaveClicked()
        {
            LeaveRequested?.Invoke();
        }

        private void OnPlayAgainClicked()
        {
            PlayAgainRequested?.Invoke();
        }
    }
}
