using System;
using UnityEngine;
using RD.Haptics;
using RD.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed partial class DefeatPresenter : IStartable, IDisposable
    {
        private readonly ISessionHudView _view;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly IRewardedAdsService _rewardedAdsService;
        private readonly IPlayerProfileService _playerProfileService;
        private readonly ILocalizationService _localization;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly IRunResultView _resultView;
        private readonly IRunStats _runStats;
        private readonly IProgressionService _progression;
        private readonly IAnalyticsService _analytics;
		private readonly IRunCompletionService _runCompletion;
        private int _finalScore;
        private Player _localPlayer;
        private RunReward _lastReward;
        private float _peakCapacity;
        private bool _isDefeated;
        private bool _continueInProgress;
        private bool _continueUsedThisLife;
        private bool _doubleDustInProgress;
        private bool _doubleDustClaimed;

        public DefeatPresenter(
            ISessionHudView view,
            ILocalPlayerProvider localPlayerProvider,
            IRewardedAdsService rewardedAdsService,
            IPlayerProfileService playerProfileService,
            ILocalizationService localization,
            ISfxPlayer sfxPlayer,
            IRunResultView resultView,
            IRunStats runStats,
            IProgressionService progression,
            IAnalyticsService analytics,
			IRunCompletionService runCompletion)
        {
			_runCompletion = runCompletion ?? throw new ArgumentNullException(nameof(runCompletion));
            _resultView = resultView ?? throw new ArgumentNullException(nameof(resultView));
            _runStats = runStats ?? throw new ArgumentNullException(nameof(runStats));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _rewardedAdsService = rewardedAdsService ?? throw new ArgumentNullException(nameof(rewardedAdsService));
            _playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _sfxPlayer = sfxPlayer ?? throw new ArgumentNullException(nameof(sfxPlayer));
        }

        public void Start()
        {
            RegisterMigrationDefeat();
            _view.PlayAgainRequested += OnPlayAgainRequested;
            _view.ContinueRequested += OnContinueRequested;
            _resultView.DoubleDustRequested += OnDoubleDustRequested;
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _migration?.UnregisterLocal("defeat");
            _view.PlayAgainRequested -= OnPlayAgainRequested;
            _view.ContinueRequested -= OnContinueRequested;
            _resultView.DoubleDustRequested -= OnDoubleDustRequested;
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

            if (_localPlayer.WasMigrationRestored)
			{
				_continueUsedThisLife = _localPlayer.ContinueUsed;
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

            _finalScore = finalScore;
            RenderDefeatTexts();
			bool canContinue = !_continueUsedThisLife && _rewardedAdsService.CanShowAd;
            _view.SetContinueVisible(canContinue);
            _view.SetLeaveButtonInteractable(true);
            if (canContinue)
            {
                _analytics.Track(AnalyticsEvents.AdOffered, ("placement", "continue"));
            }

			RunSummary summary = _runStats.Current;
			_lastReward = _runCompletion.Complete(
				summary, _runStats.KillerTier, _restoredReward ? _lastReward : null);
			_doubleDustClaimed = _restoredReward && _doubleDustClaimed;
			_restoredReward = false;
			RenderRunResult(summary);
        }

		private void RenderDefeatTexts()
		{
			string score = _localization.Get(
				LocalizationKeys.HudFinalScore, _finalScore.ToString("N0"), _playerProfileService.BestScore.ToString("N0"));
			_view.ShowDefeat(_localization.Get(LocalizationKeys.HudYouLost), score);
		}

		private void RenderRunResult(RunSummary summary)
		{
			string details = _localization.Get(
				LocalizationKeys.ResultDetails, FormatDuration(summary.DurationSeconds), summary.Kills, summary.PeakTier);
			if (!string.IsNullOrEmpty(_runStats.KillerName))
			{
				details += "\n" + _localization.Get(LocalizationKeys.ResultKilledBy, _runStats.KillerName);
			}

			_resultView.ShowRunResult(
				details,
				_localization.Get(LocalizationKeys.ResultRank, _lastReward.RankAfter),
				XpRules.Progress(_lastReward.XpBefore),
				XpRules.Progress(_lastReward.XpAfter),
				_lastReward.RankedUp,
				BuildRewardText(_doubleDustClaimed
					? _lastReward.DustGained * DustRules.DoubleMultiplier
					: _lastReward.DustGained));

			bool canDouble = !_doubleDustClaimed && _lastReward.DustGained > 0 && _rewardedAdsService.CanShowAd;
			_resultView.SetDoubleDust(canDouble, _localization.Get(LocalizationKeys.ResultDoubleDust));
			if (canDouble)
			{
				_analytics.Track(AnalyticsEvents.AdOffered, ("placement", "dust_x2"));
			}
		}

        private string BuildRewardText(int dust)
        {
            string text = _localization.Get(LocalizationKeys.ResultReward, _lastReward.XpGained, dust);
            return _lastReward.RankedUp
                ? _localization.Get(LocalizationKeys.ResultRankUp) + "\n" + text
                : text;
        }

        private static string FormatDuration(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        private void OnDoubleDustRequested()
        {
            if (_doubleDustInProgress || _doubleDustClaimed || _lastReward.DustGained <= 0)
            {
                return;
            }

            _doubleDustInProgress = true;
            _rewardedAdsService.Show(granted =>
            {
                _doubleDustInProgress = false;
                _analytics.Track(AnalyticsEvents.AdResult, ("placement", "dust_x2"), ("accepted", 1), ("completed", granted ? 1 : 0));
                if (!granted || !_isDefeated)
                {
                    return;
                }

                _doubleDustClaimed = true;
                _progression.AddDust(_lastReward.DustGained);
                _resultView.SetRewardText(BuildRewardText(_lastReward.DustGained * DustRules.DoubleMultiplier));
                _resultView.SetDoubleDust(false, string.Empty);
            });
        }

        private void OnRevived()
        {
			_continueInProgress = false;
            _isDefeated = false;
            _view.HideDefeat();
        }

        private void OnPlayAgainRequested()
        {
            if (!_isDefeated || _localPlayer == null)
            {
                return;
            }

            PlayAgain();
        }

        private void PlayAgain()
        {
			if (_continueInProgress || !_isDefeated || _localPlayer == null)
            {
                return;
            }

            _peakCapacity = 0f;
            _continueUsedThisLife = false;

            _view.HideDefeat();
            _localPlayer.RespawnRpc();
            _runStats.BeginRun();
        }

        private void OnContinueRequested()
        {
			if (_continueInProgress || !_isDefeated || _continueUsedThisLife || _localPlayer == null)
            {
                return;
            }

            _continueInProgress = true;
            _rewardedAdsService.Show(granted =>
            {
                _analytics.Track(AnalyticsEvents.AdResult, ("placement", "continue"), ("accepted", 1), ("completed", granted ? 1 : 0));
				if (!granted || !_isDefeated || _localPlayer == null)
				{
					_continueInProgress = false;
					return;
				}

				_continueUsedThisLife = true;
                _localPlayer.ContinueRpc();
            });
        }
    }
}
