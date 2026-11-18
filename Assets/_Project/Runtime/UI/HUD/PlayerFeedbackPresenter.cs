using System;
using UnityEngine;
using RD.Haptics;
using RD.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class PlayerFeedbackPresenter : IStartable, ITickable, IDisposable
    {
        private const float MuteAfterReviveSeconds = 0.5f;
        private const float GrowthThreshold = 0.0001f;
        private const float BorderWarningDistance = 18f;
        private const float BorderWarningMaxAlpha = 0.75f;
        private const float SlingshotPitch = 1.7f;
        private const float SlingshotVolume = 0.7f;

        private readonly ISessionHudView _view;
		private readonly IDamageFeedbackView _damageView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly ILocalizationService _localization;
        private readonly ISfxPlayer _sfxPlayer;
        private Player _localPlayer;
        private float _lastCapacity;
        private int _lastTier = TierRules.MinTier;
        private float _mutedUntil;
        private bool _controlsHintShown;

        public PlayerFeedbackPresenter(
            ISessionHudView view,
            ILocalPlayerProvider localPlayerProvider,
            ILocalizationService localization,
            ISfxPlayer sfxPlayer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
			_damageView = view as IDamageFeedbackView;
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
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
        }

		private bool IsFeedbackMuted =>
			_localPlayer == null || _localPlayer.IsDefeated || Time.unscaledTime < _mutedUntil;

        private void BindPlayer(Player player)
        {
            if (_localPlayer != null)
            {
                _localPlayer.Killed -= OnLocalKill;
                _localPlayer.Revived -= OnRevived;
                _localPlayer.CapacityChanged -= OnCapacityChanged;
                _localPlayer.Damaged -= OnDamaged;
                _localPlayer.TierChanged -= OnTierChanged;
                _localPlayer.MoonStruck -= OnMoonStruck;
                _localPlayer.Slingshot -= OnSlingshot;
            }

			_damageView?.ClearDamage();
			_localPlayer = player;
            if (_localPlayer == null)
            {
                return;
            }

            _localPlayer.Killed += OnLocalKill;
            _localPlayer.Revived += OnRevived;
            _localPlayer.CapacityChanged += OnCapacityChanged;
            _localPlayer.Damaged += OnDamaged;
            _localPlayer.TierChanged += OnTierChanged;
            _localPlayer.MoonStruck += OnMoonStruck;
            _localPlayer.Slingshot += OnSlingshot;
            _lastCapacity = _localPlayer.Capacity;
            _lastTier = _localPlayer.Tier;
            ShowControlsHint();
        }

        private void OnTierChanged(int tier)
        {
            bool tierUp = tier > _lastTier;
            _lastTier = tier;
            if (!tierUp || IsFeedbackMuted)
            {
                return;
            }

            _sfxPlayer.Play(SfxId.TierUp);
            Haptics.Play(HapticPreset.Success);
        }

        private void OnSlingshot()
        {
            if (IsFeedbackMuted)
            {
                return;
            }

            _sfxPlayer.Play(SfxId.Eat, SlingshotPitch, SlingshotVolume);
            Haptics.Play(HapticPreset.LightImpact);
        }

        private void OnMoonStruck(Vector2 _)
        {
            if (IsFeedbackMuted)
            {
                return;
            }

            _sfxPlayer.Play(SfxId.MoonHit);
            Haptics.Play(HapticPreset.LightImpact);
        }

        private void ShowControlsHint()
        {
            if (_controlsHintShown)
            {
                return;
            }

            _controlsHintShown = true;
            _view.ShowHint(_localization.Get(LocalizationKeys.HudHintTouch));
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
			_damageView?.ClearDamage();
			_mutedUntil = Time.unscaledTime + MuteAfterReviveSeconds;
            _lastCapacity = _localPlayer != null ? _localPlayer.Capacity : 0f;
            _lastTier = _localPlayer != null ? _localPlayer.Tier : TierRules.MinTier;
        }

        private void OnCapacityChanged(float capacity)
        {
            float delta = capacity - _lastCapacity;
            _lastCapacity = capacity;
            if (IsFeedbackMuted)
            {
                return;
            }

            if (delta > GrowthThreshold)
            {
                _sfxPlayer.Play(SfxId.Eat, Mathf.Clamp(1.4f - capacity, 0.8f, 1.7f));
            }
        }

        private void OnDamaged(float lost, float before)
		{
			if (IsFeedbackMuted)
			{
				return;
			}
			_sfxPlayer.Play(SfxId.Hit);
			Haptics.Play(HapticPreset.LightImpact);
			_damageView?.ShowDamage(GetLocalPlayerScreenPosition(), Constants.CapacityToScore(lost), lost / before);
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
