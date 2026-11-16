using System;
using RD.Localization;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace PlanetIO.UI.Hud
{
    public enum PingQuality
    {
        Hidden,
        Good,
        Fair,
        Poor
    }

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
        void SetPing(int milliseconds, PingQuality quality);
        void SetControlsSwapped(bool swapped);
        void SetFixedJoystick(bool fixedJoystick);
    }

    public interface IRunResultView
    {
        event Action DoubleDustRequested;

        void ShowRunResult(
            string details,
            string rankText,
            float fromProgress,
            float toProgress,
            bool rankedUp,
            string rewardText);

        void SetRewardText(string rewardText);
        void SetDoubleDust(bool visible, string label);
    }

    public interface IWorldHudView
    {
        void ShowEventBanner(string text);
        void SetBlitzTimer(string text, bool visible);
    }

    public interface IPerkView
    {
        event Action<int> PerkChosen;

        void ShowPerkOffer(string title, string[] names, string[] descriptions);
        void SetPerkTimer(float fraction);
        void HidePerkOffer();
        void SetActivePerks(int[] ids, string[] names);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SessionHudView : MonoBehaviour, ISessionHudView, IPerkView, IRunResultView, IWorldHudView
    {
        private const int PerkCardCount = PerkRules.OfferSize;
        private const float RankAnimationSeconds = 1.2f;
        private const float EventBannerSeconds = 5f;

        private const float KillFeedFadeSeconds = 3f;
        private const float ScorePopupSeconds = 0.9f;
        private const float ScorePopupRiseSpeed = 80f;
        private const float HintFadeSeconds = 4f;
        private const string ControlsSwappedClass = "controls-swapped";

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
        private Label _pingText;
        private Button _leaveButton;
        private Button _settingsButton;
        private Button _playAgainButton;
        private Button _continueButton;
        private Button _defeatLeaveButton;
        private Label _eventBannerText;
        private Label _blitzTimerText;
        private float _eventBannerRemaining;
        private Label _resultDetails;
        private Label _rankText;
        private Label _rewardText;
        private VisualElement _rankFill;
        private Button _doubleDustButton;
        private float _rankFrom;
        private float _rankTo;
        private float _rankAnimationTime;
        private bool _rankUp;
        private bool _rankAnimating;
        private VisualElement _perkPanel;
        private VisualElement _perkStrip;
        private VisualElement _perkTimerFill;
        private Label _perkTitle;
        private readonly VisualElement[] _perkCards = new VisualElement[PerkCardCount];
        private readonly Label[] _perkNames = new Label[PerkCardCount];
        private readonly Label[] _perkDescriptions = new Label[PerkCardCount];

        private float _killFeedTimeRemaining;
        private float _scorePopupTimeRemaining;
        private float _hintTimeRemaining;
        private Vector2 _scorePopupOrigin;
        private bool _defeatVisible;

        public event Action LeaveRequested;
        public event Action PlayAgainRequested;
        public event Action ContinueRequested;
        public event Action SettingsRequested;
        public event Action<int> PerkChosen;
        public event Action DoubleDustRequested;

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
            _pingText = _root.Q<Label>("ping-text");
            _pingText.style.display = DisplayStyle.None;
            _leaveButton = _root.Q<Button>("leave-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _playAgainButton = _root.Q<Button>("play-again");
            _continueButton = _root.Q<Button>("continue-button");
            _defeatLeaveButton = _root.Q<Button>("defeat-leave");

            _eventBannerText = _root.Q<Label>("event-banner");
            _blitzTimerText = _root.Q<Label>("blitz-timer");
            _eventBannerText.style.opacity = 0f;
            _blitzTimerText.style.display = DisplayStyle.None;
            _eventBannerRemaining = 0f;
            _resultDetails = _root.Q<Label>("result-details");
            _rankText = _root.Q<Label>("rank-text");
            _rewardText = _root.Q<Label>("reward-text");
            _rankFill = _root.Q<VisualElement>("rank-fill");
            _doubleDustButton = _root.Q<Button>("double-dust");
            _doubleDustButton.clicked += OnDoubleDustClicked;
            _rankAnimating = false;

            _perkPanel = _root.Q<VisualElement>("perk-panel");
            _perkStrip = _root.Q<VisualElement>("perk-strip");
            _perkTitle = _root.Q<Label>("perk-title");
            _perkTimerFill = _root.Q<VisualElement>("perk-timer-fill");
            for (int index = 0; index < PerkCardCount; index++)
            {
                _perkCards[index] = _root.Q<VisualElement>($"perk-card-{index}");
                _perkNames[index] = _root.Q<Label>($"perk-name-{index}");
                _perkDescriptions[index] = _root.Q<Label>($"perk-desc-{index}");
                _perkCards[index].userData = index;
                _perkCards[index].RegisterCallback<ClickEvent>(OnPerkCardClicked);
            }

            _perkPanel.style.display = DisplayStyle.None;
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
            _labels.Bind(_root.Q<Label>("loading-title"), LocalizationKeys.StatusLoading);
        }

        private void OnDisable()
        {
            _leaveButton.clicked -= OnLeaveClicked;
            _settingsButton.clicked -= OnSettingsClicked;
            _defeatLeaveButton.clicked -= OnLeaveClicked;
            _playAgainButton.clicked -= OnPlayAgainClicked;
            _continueButton.clicked -= OnContinueClicked;
            _doubleDustButton.clicked -= OnDoubleDustClicked;
            for (int index = 0; index < PerkCardCount; index++)
            {
                _perkCards[index]?.UnregisterCallback<ClickEvent>(OnPerkCardClicked);
            }

            _labels?.Dispose();
            _labels = null;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            UpdateRankAnimation(deltaTime);

            if (_eventBannerRemaining > 0f)
            {
                _eventBannerRemaining -= deltaTime;
                _eventBannerText.style.opacity = Mathf.Clamp01(_eventBannerRemaining / (EventBannerSeconds * 0.4f));
            }

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

        public void SetControlsSwapped(bool swapped)
        {
            _root.EnableInClassList(ControlsSwappedClass, swapped);
        }

        public void SetFixedJoystick(bool fixedJoystick)
        {
            GetComponent<OnScreenStickView>().SetFixedJoystick(fixedJoystick);
        }

        public void SetPing(int milliseconds, PingQuality quality)
        {
            bool visible = quality != PingQuality.Hidden;
            _pingText.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _pingText.EnableInClassList("ping-good", quality == PingQuality.Good);
            _pingText.EnableInClassList("ping-fair", quality == PingQuality.Fair);
            _pingText.EnableInClassList("ping-poor", quality == PingQuality.Poor);
            if (visible)
            {
                _pingText.text = $"{milliseconds} ms";
            }
        }

        public void ShowEventBanner(string text)
        {
            _eventBannerText.text = text;
            _eventBannerText.style.opacity = 1f;
            _eventBannerRemaining = EventBannerSeconds;
        }

        public void SetBlitzTimer(string text, bool visible)
        {
            _blitzTimerText.text = text ?? string.Empty;
            _blitzTimerText.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowRunResult(
            string details,
            string rankText,
            float fromProgress,
            float toProgress,
            bool rankedUp,
            string rewardText)
        {
            _resultDetails.text = details;
            _rankText.text = rankText;
            _rewardText.text = rewardText;
            _rankFrom = Mathf.Clamp01(fromProgress);
            _rankTo = Mathf.Clamp01(toProgress);
            _rankUp = rankedUp;
            _rankAnimationTime = 0f;
            _rankAnimating = true;
            SetRankFill(_rankFrom);
        }

        public void SetRewardText(string rewardText)
        {
            _rewardText.text = rewardText;
        }

        public void SetDoubleDust(bool visible, string label)
        {
            _doubleDustButton.text = label;
            _doubleDustButton.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void UpdateRankAnimation(float deltaTime)
        {
            if (!_rankAnimating)
            {
                return;
            }

            _rankAnimationTime += deltaTime;
            float t = Mathf.Clamp01(_rankAnimationTime / RankAnimationSeconds);
            float value;
            if (_rankUp)
            {
                value = t < 0.5f
                    ? Mathf.Lerp(_rankFrom, 1f, t / 0.5f)
                    : Mathf.Lerp(0f, _rankTo, (t - 0.5f) / 0.5f);
            }
            else
            {
                value = Mathf.Lerp(_rankFrom, _rankTo, t);
            }

            SetRankFill(value);
            if (t >= 1f)
            {
                _rankAnimating = false;
            }
        }

        private void SetRankFill(float progress)
        {
            _rankFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }

        private void OnDoubleDustClicked() => DoubleDustRequested?.Invoke();

        public void ShowPerkOffer(string title, string[] names, string[] descriptions)
        {
            _perkTitle.text = title;
            for (int index = 0; index < PerkCardCount; index++)
            {
                bool available = index < names.Length;
                _perkCards[index].style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
                if (available)
                {
                    _perkNames[index].text = names[index];
                    _perkDescriptions[index].text = descriptions[index];
                }
            }

            SetPerkTimer(1f);
            _perkPanel.style.display = DisplayStyle.Flex;
        }

        public void SetPerkTimer(float fraction)
        {
            _perkTimerFill.style.width = Length.Percent(Mathf.Clamp01(fraction) * 100f);
        }

        public void HidePerkOffer()
        {
            _perkPanel.style.display = DisplayStyle.None;
        }

        public void SetActivePerks(int[] ids, string[] names)
        {
            _perkStrip.Clear();
            for (int index = 0; index < ids.Length; index++)
            {
                string name = names[index];
                Label chip = new(string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpperInvariant())
                {
                    pickingMode = PickingMode.Ignore
                };
                chip.AddToClassList("perk-chip");
                chip.AddToClassList($"perk-chip--{ids[index]}");
                _perkStrip.Add(chip);
            }
        }

        private void OnPerkCardClicked(ClickEvent evt)
        {
            if (evt.currentTarget is VisualElement { userData: int index })
            {
                PerkChosen?.Invoke(index);
            }
        }

        private void OnLeaveClicked() => LeaveRequested?.Invoke();

        private void OnSettingsClicked() => SettingsRequested?.Invoke();

        private void OnPlayAgainClicked() => PlayAgainRequested?.Invoke();

        private void OnContinueClicked() => ContinueRequested?.Invoke();
    }
}
