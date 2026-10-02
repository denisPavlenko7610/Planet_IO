using System;
using UnityEngine;
using UnityTemplates.Haptics;
using UnityTemplates.Localization;
using UnityTemplates.Settings;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class PlayerFeedbackPresenter : IStartable, ITickable, IDisposable
    {
        private const float MuteAfterReviveSeconds = 0.5f;
        private const float GrowthThreshold = 0.0001f;
        private const float DamageThreshold = -0.005f;
        private const float BorderWarningDistance = 18f;
        private const float BorderWarningMaxAlpha = 0.75f;
        private const float TutorialStepSeconds = 5f;

        private static readonly string[] TutorialSteps =
        {
            LocalizationKeys.TutorialEat,
            LocalizationKeys.TutorialAvoid,
            LocalizationKeys.TutorialBoost
        };

        private readonly ISessionHudView _view;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly ILocalizationService _localization;
        private readonly ISettingsService _settings;
        private readonly ISfxPlayer _sfxPlayer;
        private Player _localPlayer;
        private float _lastCapacity;
        private float _mutedUntil;
        private bool _controlsHintShown;
        private int _tutorialStep = -1;
        private float _nextTutorialTime;

        public PlayerFeedbackPresenter(
            ISessionHudView view,
            ILocalPlayerProvider localPlayerProvider,
            ILocalizationService localization,
            ISettingsService settings,
            ISfxPlayer sfxPlayer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
        }

        public void Start()
        {
            Player.DefeatAnnounced += OnDefeatAnnounced;
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            Player.DefeatAnnounced -= OnDefeatAnnounced;
            _localPlayerProvider.LocalPlayerChanged -= BindPlayer;
            BindPlayer(null);
        }

        public void Tick()
        {
            UpdateBorderWarning();
            UpdateTutorial();
        }

        private void BindPlayer(Player player)
        {
            if (_localPlayer != null)
            {
                _localPlayer.Killed -= OnLocalKill;
                _localPlayer.Revived -= OnRevived;
                _localPlayer.CapacityChanged -= OnCapacityChanged;
            }

            _localPlayer = player;
            if (_localPlayer == null)
            {
                return;
            }

            _localPlayer.Killed += OnLocalKill;
            _localPlayer.Revived += OnRevived;
            _localPlayer.CapacityChanged += OnCapacityChanged;
            _lastCapacity = _localPlayer.Capacity;
            ShowControlsHint();
        }

        private void ShowControlsHint()
        {
            if (_controlsHintShown)
            {
                return;
            }

            _controlsHintShown = true;
            _view.ShowHint(_localization.Get(LocalizationKeys.HudHintTouch));

            if (!_settings.Get(GameSettingKeys.TutorialCompleted))
            {
                _tutorialStep = 0;
                _nextTutorialTime = Time.unscaledTime + TutorialStepSeconds;
            }
        }

        private void UpdateTutorial()
        {
            if (_tutorialStep < 0 || Time.unscaledTime < _nextTutorialTime || _localPlayer == null || _localPlayer.IsDefeated)
            {
                return;
            }

            _view.ShowHint(_localization.Get(TutorialSteps[_tutorialStep]));
            _tutorialStep++;
            _nextTutorialTime = Time.unscaledTime + TutorialStepSeconds;

            if (_tutorialStep >= TutorialSteps.Length)
            {
                _tutorialStep = -1;
                _settings.Set(GameSettingKeys.TutorialCompleted, true);
                _settings.Flush();
            }
        }

        private void UpdateBorderWarning()
        {
            float strength = 0f;
            if (_localPlayer != null && _localPlayer.IsSpawned && !_localPlayer.IsDefeated)
            {
                float distance = WorldBounds.DistanceToEdge(_localPlayer.transform.position);
                strength = (1f - Mathf.Clamp01(distance / BorderWarningDistance)) * BorderWarningMaxAlpha;
            }

            _view.SetBorderWarning(strength);
        }

        private void OnRevived()
        {
            _mutedUntil = Time.unscaledTime + MuteAfterReviveSeconds;
            _lastCapacity = _localPlayer != null ? _localPlayer.Capacity : 0f;
        }

        private void OnCapacityChanged(float capacity)
        {
            float delta = capacity - _lastCapacity;
            _lastCapacity = capacity;
            if (_localPlayer == null || _localPlayer.IsDefeated || Time.unscaledTime < _mutedUntil)
            {
                return;
            }

            if (delta > GrowthThreshold)
            {
                _sfxPlayer.Play(SfxId.Eat, Mathf.Clamp(1.4f - capacity, 0.8f, 1.7f));
            }
            else if (delta < DamageThreshold)
            {
                _sfxPlayer.Play(SfxId.Hit);
                Haptics.Play(HapticPreset.LightImpact);
            }
        }

        private void OnLocalKill(string victimName, int score)
        {
            _sfxPlayer.Play(SfxId.Kill);
            Haptics.Play(HapticPreset.Success);
            _view.ShowKillFeed(_localization.Get(LocalizationKeys.HudYouAte, victimName));
            _view.ShowScorePopup(GetLocalPlayerScreenPosition(), score);
        }

        private void OnDefeatAnnounced(Player.DefeatAnnouncement announcement)
        {
            if (_localPlayer != null &&
                (announcement.KillerClientId == _localPlayer.OwnerClientId ||
                 announcement.VictimClientId == _localPlayer.OwnerClientId))
            {
                return;
            }

            _view.ShowKillFeed(_localization.Get(
                LocalizationKeys.HudKillFeed, announcement.KillerName, announcement.VictimName));
        }

        private Vector2 GetLocalPlayerScreenPosition()
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null || _localPlayer == null)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            return camera.WorldToScreenPoint(_localPlayer.transform.position);
        }
    }
}
