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
        private readonly List<LeaderboardEntry> _entries = new();
        private float _refreshTimeRemaining;
        private bool _leaveInProgress;

        public GameSessionHudPresenter(
            NetworkManager networkManager,
            INetworkSessionService networkSessionService,
            ISessionHudView sessionHudView)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _sessionHudView = sessionHudView ?? throw new ArgumentNullException(nameof(sessionHudView));
        }

        public void Start()
        {
            _sessionHudView.LeaveRequested += OnLeaveRequested;
            Refresh();
        }

        public void Tick()
        {
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
        }

        private void Refresh()
        {
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

            StringBuilder builder = new();
            builder.AppendLine("<b>LEADERS</b>");
            int visibleCount = Mathf.Min(
                VisibleLeaderboardEntries,
                _entries.Count);

            for (int index = 0; index < visibleCount; index++)
            {
                LeaderboardEntry entry = _entries[index];
                string line =
                    $"{index + 1}. {EscapeRichText(entry.Name)}" +
                    $"  {entry.Score:N0}";
                builder.Append(entry.IsLocalPlayer
                    ? $"<color=#5FE0FF>{line}</color>"
                    : line);

                if (index < visibleCount - 1)
                {
                    builder.AppendLine();
                }
            }

            _sessionHudView.ShowLeaderboardText(builder.ToString());
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
                    _entries.Add(new LeaderboardEntry(
                        $"Bot {networkObject.NetworkObjectId % 100:00}",
                        ToScore(enemy.Capacity),
                        false));
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

        private static int ToScore(float capacity) =>
            Constants.CapacityToScore(capacity);

        private static string EscapeRichText(string value) =>
            (value ?? NicknameRules.DefaultNickname)
            .Replace("<", "‹")
            .Replace(">", "›");

        private readonly struct LeaderboardEntry
        {
            public LeaderboardEntry(
                string name,
                int score,
                bool isLocalPlayer)
            {
                Name = name;
                Score = score;
                IsLocalPlayer = isLocalPlayer;
            }

            public string Name { get; }
            public int Score { get; }
            public bool IsLocalPlayer { get; }
        }
    }
}
