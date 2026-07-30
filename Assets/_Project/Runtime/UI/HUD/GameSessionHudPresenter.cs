using System;
using System.Collections.Generic;
using System.Text;
using PlanetIO.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class GameSessionHudPresenter : IStartable, ITickable, IDisposable
    {
        private const float RefreshIntervalSeconds = 0.5f;
        private const int VisibleLeaderboardEntries = 6;

        private readonly NetworkManager _networkManager;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ISessionHudView _sessionHudView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly List<(string Name, int Score)> _entries = new();
        private readonly StringBuilder _leaderboardBuilder = new();
        private Player _localPlayer;
        private float _refreshTimeRemaining;
        private bool _leaveInProgress;
        private bool _isDefeated;

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
            _sessionHudView.LeaveRequested += OnLeaveRequested;
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
            int playerCount =
                _networkManager.ConnectedClientsList?.Count ?? 0;
            _sessionHudView.ShowSessionText(
                $"{roomLabel}\nPlayers: {playerCount}/{room.MaxPlayers}");

            CollectEntries();
            _entries.Sort(static (left, right) =>
                right.Score.CompareTo(left.Score));

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
            }

            _localPlayer = player;
            if (_localPlayer == null)
            {
                return;
            }

            _localPlayer.Defeated += OnPlayerDefeated;
            if (_localPlayer.IsDefeated)
            {
                OnPlayerDefeated();
            }
        }

        private void OnPlayerDefeated()
        {
            if (_isDefeated || _localPlayer == null)
            {
                return;
            }

            _isDefeated = true;
            _sessionHudView.ShowDefeat(
                Constants.CapacityToScore(_localPlayer.Capacity));
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
                if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    _entries.Add((
                        $"Bot {networkObject.NetworkObjectId % 100:00}",
                        Constants.CapacityToScore(enemy.Capacity)));
                }
            }
        }

        private void OnLeaveRequested()
        {
            _ = LeaveAsync();
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
                // Scene or application is closing.
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception);
                _leaveInProgress = false;
                _sessionHudView.SetLeaveButtonInteractable(true);
            }
        }

    }
}
