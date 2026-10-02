using System;
using UnityEngine;
using UnityTemplates.Haptics;
using UnityTemplates.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class DefeatPresenter : IStartable, IDisposable
    {
        private readonly ISessionHudView _view;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly IRewardedAdsService _rewardedAdsService;
        private readonly IPlayerProfileService _playerProfileService;
        private readonly ILocalizationService _localization;
        private readonly ISfxPlayer _sfxPlayer;
        private Player _localPlayer;
        private float _peakCapacity;
        private bool _isDefeated;
        private bool _continueInProgress;
        private bool _continueUsedThisLife;

        public DefeatPresenter(
            ISessionHudView view,
            ILocalPlayerProvider localPlayerProvider,
            IRewardedAdsService rewardedAdsService,
            IPlayerProfileService playerProfileService,
            ILocalizationService localization,
            ISfxPlayer sfxPlayer)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _rewardedAdsService = rewardedAdsService ?? throw new ArgumentNullException(nameof(rewardedAdsService));
            _playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
        }

        public void Start()
        {
            _view.PlayAgainRequested += OnPlayAgainRequested;
            _view.ContinueRequested += OnContinueRequested;
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _view.PlayAgainRequested -= OnPlayAgainRequested;
            _view.ContinueRequested -= OnContinueRequested;
            _localPlayerProvider.LocalPlayerChanged -= BindPlayer;
            BindPlayer(null);
        }

        private void BindPlayer(Player player)
        {
            if (_localPlayer != null)
            {
                _localPlayer.Defeated -= OnDefeated;
                _localPlayer.Revived -= OnRevived;
                _localPlayer.CapacityChanged -= OnCapacityChanged;
            }

            _localPlayer = player;
            if (_localPlayer == null)
            {
                return;
            }

            _localPlayer.Defeated += OnDefeated;
            _localPlayer.Revived += OnRevived;
            _localPlayer.CapacityChanged += OnCapacityChanged;
            _peakCapacity = Mathf.Max(_peakCapacity, _localPlayer.Capacity);

            if (_localPlayer.IsDefeated)
            {
                OnDefeated();
            }
        }

        private void OnCapacityChanged(float capacity)
        {
            if (!_isDefeated)
            {
                _peakCapacity = Mathf.Max(_peakCapacity, capacity);
            }
        }

        private void OnDefeated()
        {
            if (_isDefeated || _localPlayer == null)
            {
                return;
            }

            _isDefeated = true;
            _sfxPlayer.Play(SfxId.Death);
            Haptics.Play(HapticPreset.Failure);

            int finalScore = Constants.CapacityToScore(Mathf.Max(_peakCapacity, _localPlayer.Capacity));
            _playerProfileService.SubmitScore(finalScore);

            _view.ShowDefeat(
                _localization.Get(LocalizationKeys.HudYouLost),
                _localization.Get(LocalizationKeys.HudFinalScore, finalScore.ToString("N0"),
                    _playerProfileService.BestScore.ToString("N0")));
            _view.SetContinueVisible(!_continueUsedThisLife && _rewardedAdsService.CanShowAd);
            _view.SetLeaveButtonInteractable(true);
        }

        private void OnRevived()
        {
            _isDefeated = false;
            _view.HideDefeat();
        }

        private void OnPlayAgainRequested()
        {
            if (!_isDefeated || _localPlayer == null)
            {
                return;
            }

            _peakCapacity = 0f;
            _continueUsedThisLife = false;
            _view.HideDefeat();
            _localPlayer.RespawnRpc();
        }

        private void OnContinueRequested()
        {
            if (_continueInProgress || _localPlayer == null)
            {
                return;
            }

            _continueInProgress = true;
            _rewardedAdsService.Show(granted =>
            {
                _continueInProgress = false;
                if (!granted || !_isDefeated || _localPlayer == null)
                {
                    return;
                }

                _continueUsedThisLife = true;
                _view.HideDefeat();
                _localPlayer.ContinueRpc();
            });
        }
    }
}
