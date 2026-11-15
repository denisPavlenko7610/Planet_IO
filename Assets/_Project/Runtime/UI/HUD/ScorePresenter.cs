using System;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class ScorePresenter : IStartable, IDisposable
    {
        private readonly IScoreView _scoreView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private Player _boundPlayer;

        public ScorePresenter(IScoreView scoreView, ILocalPlayerProvider localPlayerProvider)
        {
            _scoreView = scoreView ?? throw new ArgumentNullException(nameof(scoreView));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
        }

        public void Start()
        {
            _localPlayerProvider.LocalPlayerChanged += OnLocalPlayerChanged;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _localPlayerProvider.LocalPlayerChanged -= OnLocalPlayerChanged;
            BindPlayer(null);
        }

        private void OnLocalPlayerChanged(Player player)
        {
            BindPlayer(player);
        }

        private void BindPlayer(Player player)
        {
            if (_boundPlayer != null)
            {
                _boundPlayer.CapacityChanged -= OnCapacityChanged;
                _boundPlayer.TierChanged -= OnTierChanged;
            }

            _boundPlayer = player;
            if (_boundPlayer == null)
            {
                _scoreView.ShowScore(0);
                _scoreView.ShowTier(TierRules.MinTier, 0f);
                return;
            }

            _boundPlayer.CapacityChanged += OnCapacityChanged;
            _boundPlayer.TierChanged += OnTierChanged;
            OnCapacityChanged(_boundPlayer.Capacity);
        }

        private void OnCapacityChanged(float capacity)
        {
            int score = Constants.CapacityToScore(capacity);
            _scoreView.ShowScore(score);
            ShowTier(capacity);
        }

        private void OnTierChanged(int _)
        {
            if (_boundPlayer != null)
            {
                ShowTier(_boundPlayer.Capacity);
            }
        }

        private void ShowTier(float capacity)
        {
            int tier = _boundPlayer.Tier;
            _scoreView.ShowTier(tier, TierRules.Progress(capacity, tier));
        }
    }
}
