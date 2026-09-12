using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class GameSessionHudPresenter : IStartable, ITickable, IDisposable
    {
        private const float RefreshIntervalSeconds = 0.5f;
        private const int VisibleLeaderboardEntries = 6;
        private const string BestScoreKey = "PlanetIO.BestScore";

        private readonly NetworkManager _networkManager;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ISessionHudView _sessionHudView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly List<(string Name, int Score)> _entries = new();
        private readonly StringBuilder _leaderboardBuilder = new();
        private Player _localPlayer;
        private float _refreshTimeRemaining;
        private float _lastCapacity;
        private bool _leaveInProgress;
        private bool _restartInProgress;
        private bool _isDefeated;
        private AudioClip _eatClip;
        private AudioClip _hitClip;
        private AudioClip _killClip;
        private AudioClip _deathClip;

        public GameSessionHudPresenter(
            NetworkManager networkManager,
            INetworkSessionService networkSessionService,
            ISessionHudView sessionHudView,
            ILocalPlayerProvider localPlayerProvider)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _sessionHudView = sessionHudView ?? throw new ArgumentNullException(nameof(sessionHudView));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
        }

        public void Start()
        {
            _eatClip = GameAudio.Load("eat");
            _hitClip = GameAudio.Load("hit");
            _killClip = GameAudio.Load("kill");
            _deathClip = GameAudio.Load("death");

            _sessionHudView.LeaveRequested += OnLeaveRequested;
            _sessionHudView.PlayAgainRequested += OnPlayAgainRequested;
            _localPlayerProvider.LocalPlayerChanged += OnLocalPlayerChanged;
            BindPlayer(_localPlayerProvider.LocalPlayer);
            Refresh();
            _refreshTimeRemaining = RefreshIntervalSeconds;
        }

        public void Tick()
        {
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
            _sessionHudView.PlayAgainRequested -= OnPlayAgainRequested;
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
                ? "SINGLE PLAYER"
                : $"ROOM {room.RoomCode}";

            CollectEntries();
            _entries.Sort(static (left, right) =>
                right.Score.CompareTo(left.Score));

            int totalEntries = _entries.Count;
            int localRank = 0;
            if (_localPlayer != null && _localPlayer.IsSpawned)
            {
                int localScore = Constants.CapacityToScore(_localPlayer.Capacity);
                foreach ((string Name, int Score) entry in _entries)
                {
                    if (entry.Score > localScore)
                    {
                        localRank++;
                    }
                }

                localRank++;
            }

            string rankLine = localRank > 0
                ? $"\nRank: #{localRank}/{totalEntries}"
                : string.Empty;
            int playerCount =
                _networkManager.ConnectedClientsList?.Count ?? 0;
            _sessionHudView.ShowSessionText(
                $"{roomLabel}\nPlayers: {playerCount}/{room.MaxPlayers}{rankLine}");

            _leaderboardBuilder.Clear();
            _leaderboardBuilder.AppendLine("<b>LEADERS</b>");
            int visibleCount = Mathf.Min(
                VisibleLeaderboardEntries,
                _entries.Count);

            for (int index = 0; index < visibleCount; index++)
            {
                (string Name, int Score) entry = _entries[index];
                _leaderboardBuilder
                    .Append(index + 1)
                    .Append(". ")
                    .Append(entry.Name)
                    .Append("  ")
                    .Append(entry.Score.ToString("N0"));

                if (index < visibleCount - 1)
                {
                    _leaderboardBuilder.AppendLine();
                }
            }

            _sessionHudView.ShowLeaderboardText(
                _leaderboardBuilder.ToString());
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
            if (_localPlayer.IsDefeated)
            {
                OnPlayerDefeated();
            }
        }

        private void OnLocalCapacityChanged(float capacity)
        {
            float delta = capacity - _lastCapacity;
            _lastCapacity = capacity;

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
            _sessionHudView.ShowKillFeed($"You ate {victimName}");
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

            int finalScore = Constants.CapacityToScore(_localPlayer.Capacity);
            int bestScore = Mathf.Max(finalScore, PlayerPrefs.GetInt(BestScoreKey, 0));
            PlayerPrefs.SetInt(BestScoreKey, bestScore);

            bool canPlayAgain =
                _networkSessionService.Mode == NetworkSessionMode.SinglePlayer;
            _sessionHudView.ShowDefeat(finalScore, bestScore, canPlayAgain);
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
                            Constants.CapacityToScore(player.Capacity)));
                    }

                    continue;
                }

                if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    _entries.Add((
                        enemy.DisplayName,
                        Constants.CapacityToScore(enemy.Capacity)));
                }
            }
        }

        private void OnLeaveRequested()
        {
            _ = LeaveAsync();
        }

        private void OnPlayAgainRequested()
        {
            if (_restartInProgress || _leaveInProgress)
            {
                return;
            }

            _restartInProgress = true;
            _sessionHudView.SetLeaveButtonInteractable(false);
            _sessionHudView.SetPlayAgainVisible(false);
            _ = RestartSinglePlayerAsync();
        }

        private async Awaitable RestartSinglePlayerAsync()
        {
            try
            {
                await _networkSessionService.ShutdownAndReturnToMenuAsync();
                await _networkSessionService.StartSinglePlayerAsync();
            }
            catch (OperationCanceledException)
            {
                // Scene or application is closing.
            }
            catch (Exception exception)
            {
                GameLogger.LogException(exception);
            }
        }

        private async Awaitable LeaveAsync()
        {
            if (_leaveInProgress)
            {
                return;
            }

            _leaveInProgress = true;
            _sessionHudView.SetLeaveButtonInteractable(false);
            _sessionHudView.SetPlayAgainVisible(false);

            try
            {
                await _networkSessionService
                    .ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
                // Scene or application is closing.
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
