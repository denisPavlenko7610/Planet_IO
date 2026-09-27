using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;
using UnityTemplates.Haptics;
using UnityTemplates.Localization;

namespace PlanetIO.UI.Hud
{
    public sealed class GameSessionHudPresenter : IStartable, ITickable, IDisposable
    {
        private const float RefreshIntervalSeconds = 0.5f;
        private const int VisibleLeaderboardEntries = 6;
        private const string LocalEntryColor = "#FFE066";
        private const float FeedbackMuteAfterResumeSeconds = 0.5f;
        private const float BorderWarningDistance = 18f;
        private const float BorderWarningMaxAlpha = 0.75f;

        private readonly NetworkManager _networkManager;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ISessionHudView _sessionHudView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly IRewardedAdsService _rewardedAdsService;
        private readonly IPlayerProfileService _playerProfileService;
        private readonly ILocalizationService _localization;
        private readonly List<(string Name, int Score, bool IsLocal)> _entries = new();
        private readonly StringBuilder _leaderboardBuilder = new();
        private Player _localPlayer;
        private float _refreshTimeRemaining;
        private float _lastCapacity;
        private float _peakCapacity;
        private bool _leaveInProgress;
        private bool _continueInProgress;
        private bool _continueUsedThisLife;
        private float _feedbackMutedUntil;
        private bool _isDefeated;
        private bool _hintShown;
        private AudioClip _eatClip;
        private AudioClip _hitClip;
        private AudioClip _killClip;
        private AudioClip _deathClip;

        public GameSessionHudPresenter(
            NetworkManager networkManager,
            INetworkSessionService networkSessionService,
            ISessionHudView sessionHudView,
            ILocalPlayerProvider localPlayerProvider,
            IRewardedAdsService rewardedAdsService,
            IPlayerProfileService playerProfileService,
            ILocalizationService localization)
        {
            _playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _sessionHudView = sessionHudView ?? throw new ArgumentNullException(nameof(sessionHudView));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _rewardedAdsService = rewardedAdsService ?? throw new ArgumentNullException(nameof(rewardedAdsService));
        }

        public void Start()
        {
            _eatClip = GameAudio.Load("eat");
            _hitClip = GameAudio.Load("hit");
            _killClip = GameAudio.Load("kill");
            _deathClip = GameAudio.Load("death");

            _sessionHudView.LeaveRequested += OnLeaveRequested;
            _localization.LocaleChanged += OnLocaleChanged;
            RenderButtonLabels();
            _sessionHudView.PlayAgainRequested += OnPlayAgainRequested;
            _sessionHudView.ContinueRequested += OnContinueRequested;
            _localPlayerProvider.LocalPlayerChanged += OnLocalPlayerChanged;
            BindPlayer(_localPlayerProvider.LocalPlayer);
            Refresh();
            _refreshTimeRemaining = RefreshIntervalSeconds;
        }

        public void Tick()
        {
            UpdateBorderWarning();

            if (_isDefeated)
            {
                return;
            }

            _refreshTimeRemaining -= Time.unscaledDeltaTime;
            if (_refreshTimeRemaining > 0f)
            {
                return;
            }

            _refreshTimeRemaining = RefreshIntervalSeconds;
            Refresh();
        }

        public void Dispose()
        {
            _sessionHudView.LeaveRequested -= OnLeaveRequested;
            _localization.LocaleChanged -= OnLocaleChanged;
            _sessionHudView.PlayAgainRequested -= OnPlayAgainRequested;
            _sessionHudView.ContinueRequested -= OnContinueRequested;
            _localPlayerProvider.LocalPlayerChanged -= OnLocalPlayerChanged;
            BindPlayer(null);
        }

        private void Refresh()
        {
            if (_isDefeated)
            {
                return;
            }

            RoomConnectionSettings room =
                _networkSessionService.CurrentRoom;
            string roomLabel = _networkSessionService.Mode ==
                               NetworkSessionMode.SinglePlayer
                ? _localization.Get(LocalizationKeys.HudSinglePlayer)
                : _localization.Get(LocalizationKeys.HudRoom, room.RoomCode);

            CollectEntries();
            _entries.Sort(static (left, right) =>
                right.Score.CompareTo(left.Score));

            int totalEntries = _entries.Count;
            int localRank = 0;
            if (_localPlayer != null && _localPlayer.IsSpawned)
            {
                int localScore = Constants.CapacityToScore(_localPlayer.Capacity);
                foreach ((string Name, int Score, bool IsLocal) entry in _entries)
                {
                    if (entry.Score > localScore)
                    {
                        localRank++;
                    }
                }

                localRank++;
            }

            string rankLine = localRank > 0
                ? "\n" + _localization.Get(LocalizationKeys.HudRank, localRank, totalEntries)
                : string.Empty;
            int playerCount =
                _networkManager.ConnectedClientsList?.Count ?? 0;
            string playersLine = _networkSessionService.Mode == NetworkSessionMode.SinglePlayer
                ? string.Empty
                : "\n" + _localization.Get(LocalizationKeys.HudPlayers, playerCount, room.MaxPlayers);
            _sessionHudView.ShowSessionText($"{roomLabel}{playersLine}{rankLine}");

            _leaderboardBuilder.Clear();
            _leaderboardBuilder.Append("<b>").Append(_localization.Get(LocalizationKeys.HudLeaders)).AppendLine("</b>");
            int visibleCount = Mathf.Min(
                VisibleLeaderboardEntries,
                _entries.Count);

            for (int index = 0; index < visibleCount; index++)
            {
                (string Name, int Score, bool IsLocal) entry = _entries[index];
                if (entry.IsLocal)
                {
                    _leaderboardBuilder.Append("<color=").Append(LocalEntryColor).Append('>');
                }

                _leaderboardBuilder
                    .Append(index + 1)
                    .Append(". ")
                    .Append(entry.Name)
                    .Append("  ")
                    .Append(entry.Score.ToString("N0"));

                if (entry.IsLocal)
                {
                    _leaderboardBuilder.Append("</color>");
                }

                if (index < visibleCount - 1)
                {
                    _leaderboardBuilder.AppendLine();
                }
            }

            if (localRank > visibleCount && _localPlayer != null)
            {
                _leaderboardBuilder
                    .AppendLine()
                    .Append("<color=").Append(LocalEntryColor).Append('>')
                    .Append(localRank)
                    .Append(". ")
                    .Append(_localPlayer.DisplayName)
                    .Append("  ")
                    .Append(Constants.CapacityToScore(_localPlayer.Capacity).ToString("N0"))
                    .Append("</color>");
            }

            _sessionHudView.ShowLeaderboardText(
                _leaderboardBuilder.ToString());
        }

        private void OnLocaleChanged(LocaleChanged _)
        {
            RenderButtonLabels();
            Refresh();
        }

        private void RenderButtonLabels()
        {
            _sessionHudView.SetButtonLabels(
                _localization.Get(LocalizationKeys.HudLeave),
                _localization.Get(LocalizationKeys.HudPlayAgain),
                _localization.Get(LocalizationKeys.HudWatchAd));
        }

        private void UpdateBorderWarning()
        {
            float strength = 0f;
            if (!_isDefeated && _localPlayer != null && _localPlayer.IsSpawned)
            {
                float distance = WorldBounds.DistanceToEdge(_localPlayer.transform.position);
                strength = (1f - Mathf.Clamp01(distance / BorderWarningDistance)) * BorderWarningMaxAlpha;
            }

            _sessionHudView.SetBorderWarning(strength);
        }

        private void OnLocalPlayerChanged(Player player)
        {
            BindPlayer(player);
        }

        private void BindPlayer(Player player)
        {
            if (_localPlayer != null)
            {
                _localPlayer.Defeated -= OnPlayerDefeated;
                _localPlayer.Killed -= OnLocalPlayerKill;
                _localPlayer.CapacityChanged -= OnLocalCapacityChanged;
            }

            _localPlayer = player;
            if (_localPlayer == null)
            {
                return;
            }

            _localPlayer.Defeated += OnPlayerDefeated;
            _localPlayer.Killed += OnLocalPlayerKill;
            _localPlayer.CapacityChanged += OnLocalCapacityChanged;
            _lastCapacity = _localPlayer.Capacity;
            _peakCapacity = Mathf.Max(_peakCapacity, _lastCapacity);

            if (!_hintShown)
            {
                _hintShown = true;
                _sessionHudView.ShowHint(_localization.Get(UnityEngine.Application.isMobilePlatform
                    ? LocalizationKeys.HudHintTouch
                    : LocalizationKeys.HudHintDesktop));
            }

            if (_localPlayer.IsDefeated)
            {
                OnPlayerDefeated();
            }
        }

        private void OnLocalCapacityChanged(float capacity)
        {
            float delta = capacity - _lastCapacity;
            _lastCapacity = capacity;
            if (_isDefeated)
            {
                return;
            }

            _peakCapacity = Mathf.Max(_peakCapacity, capacity);
            if (Time.unscaledTime < _feedbackMutedUntil)
            {
                return;
            }

            if (delta > 0.0001f)
            {
                GameAudio.Play2D(_eatClip, Mathf.Clamp(1.4f - capacity, 0.8f, 1.7f), 0.45f);
            }
            else if (delta < -0.005f)
            {
                GameAudio.Play2D(_hitClip, 1f, 0.55f);
            }
        }

        private void OnLocalPlayerKill(string victimName, int score)
        {
            GameAudio.Play2D(_killClip, 1f, 0.65f);
            Haptics.Play(HapticPreset.Success);
            _sessionHudView.ShowKillFeed(_localization.Get(LocalizationKeys.HudYouAte, victimName));
            _sessionHudView.ShowScorePopup(GetLocalPlayerScreenPosition(), score);
        }

        private Vector2 GetLocalPlayerScreenPosition()
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null || _localPlayer == null)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            return (Vector2)camera.WorldToScreenPoint(_localPlayer.transform.position);
        }

        private void OnPlayerDefeated()
        {
            if (_isDefeated || _localPlayer == null)
            {
                return;
            }

            _isDefeated = true;
            GameAudio.Play2D(_deathClip, 1f, 0.7f);
            Haptics.Play(HapticPreset.Failure);

            int finalScore = Constants.CapacityToScore(Mathf.Max(_peakCapacity, _localPlayer.Capacity));
            _playerProfileService.SubmitScore(finalScore);
            int bestScore = _playerProfileService.BestScore;

            _sessionHudView.ShowDefeat(
                _localization.Get(LocalizationKeys.HudYouLost),
                _localization.Get(LocalizationKeys.HudFinalScore, finalScore.ToString("N0"), bestScore.ToString("N0")));
            _sessionHudView.SetContinueVisible(!_continueUsedThisLife && _rewardedAdsService.CanShowAd);
            _sessionHudView.SetLeaveButtonInteractable(true);
        }

        private void CollectEntries()
        {
            _entries.Clear();
            if (_networkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in
                     _networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject.TryGetComponent(out Player player))
                {
                    if (!player.IsDefeated)
                    {
                        _entries.Add((
                            player.DisplayName,
                            Constants.CapacityToScore(player.Capacity),
                            player == _localPlayer));
                    }

                    continue;
                }

                if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    _entries.Add((
                        enemy.DisplayName,
                        Constants.CapacityToScore(enemy.Capacity),
                        false));
                }
            }
        }

        private void OnLeaveRequested()
        {
            _ = LeaveAsync();
        }

        private void OnPlayAgainRequested()
        {
            if (_leaveInProgress || !_isDefeated || _localPlayer == null)
            {
                return;
            }

            ResumeAfterDefeat();
            _peakCapacity = 0f;
            _continueUsedThisLife = false;
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

                ResumeAfterDefeat();
                _continueUsedThisLife = true;
                _localPlayer.ContinueRpc();
            });
        }

        private void ResumeAfterDefeat()
        {
            _isDefeated = false;
            _lastCapacity = _localPlayer.Capacity;
            _sessionHudView.HideDefeat();
            _refreshTimeRemaining = 0f;
            _feedbackMutedUntil = Time.unscaledTime + FeedbackMuteAfterResumeSeconds;
        }

        private async Awaitable LeaveAsync()
        {
            if (_leaveInProgress)
            {
                return;
            }

            _leaveInProgress = true;
            _sessionHudView.SetLeaveButtonInteractable(false);

            try
            {
                await _networkSessionService
                    .ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                GameLogger.LogException(exception);
                _leaveInProgress = false;
                _sessionHudView.SetLeaveButtonInteractable(true);
            }
        }

    }
}
