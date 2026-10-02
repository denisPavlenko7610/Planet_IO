using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityTemplates.Localization;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class LeaderboardPresenter : IStartable, ITickable, IDisposable
    {
        private const float RefreshIntervalSeconds = 0.5f;
        private const int VisibleEntries = 6;
        private const string LocalEntryColor = "#FFE066";

        private readonly NetworkManager _networkManager;
        private readonly INetworkSessionService _networkSessionService;
        private readonly ISessionHudView _view;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly ILocalizationService _localization;
        private readonly List<(string Name, int Score, bool IsLocal)> _entries = new();
        private readonly StringBuilder _builder = new();
        private float _refreshTimeRemaining;

        public LeaderboardPresenter(
            NetworkManager networkManager,
            INetworkSessionService networkSessionService,
            ISessionHudView view,
            ILocalPlayerProvider localPlayerProvider,
            ILocalizationService localization)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        public void Start()
        {
            _localization.LocaleChanged += OnLocaleChanged;
            Refresh();
        }

        public void Dispose()
        {
            _localization.LocaleChanged -= OnLocaleChanged;
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

        private void OnLocaleChanged(LocaleChanged _) => Refresh();

        private void Refresh()
        {
            Player localPlayer = _localPlayerProvider.LocalPlayer;
            CollectEntries(localPlayer);
            _entries.Sort(static (left, right) => right.Score.CompareTo(left.Score));

            int localRank = FindLocalRank();
            RenderSession(localRank);
            RenderLeaderboard(localRank, localPlayer);
        }

        private int FindLocalRank()
        {
            for (int index = 0; index < _entries.Count; index++)
            {
                if (_entries[index].IsLocal)
                {
                    return index + 1;
                }
            }

            return 0;
        }

        private void RenderSession(int localRank)
        {
            RoomConnectionSettings room = _networkSessionService.CurrentRoom;
            bool singlePlayer = _networkSessionService.Mode == NetworkSessionMode.SinglePlayer;

            _builder.Clear();
            _builder.Append(singlePlayer
                ? _localization.Get(LocalizationKeys.HudSinglePlayer)
                : _localization.Get(LocalizationKeys.HudRoom, room.RoomCode));

            if (!singlePlayer)
            {
                int playerCount = _networkManager.ConnectedClientsList?.Count ?? 0;
                _builder.Append('\n').Append(_localization.Get(LocalizationKeys.HudPlayers, playerCount, room.MaxPlayers));
            }

            if (localRank > 0)
            {
                _builder.Append('\n').Append(_localization.Get(LocalizationKeys.HudRank, localRank, _entries.Count));
            }

            _view.ShowSessionText(_builder.ToString());
        }

        private void RenderLeaderboard(int localRank, Player localPlayer)
        {
            _builder.Clear();
            _builder.Append("<b>").Append(_localization.Get(LocalizationKeys.HudLeaders)).Append("</b>");

            int visibleCount = Mathf.Min(VisibleEntries, _entries.Count);
            for (int index = 0; index < visibleCount; index++)
            {
                (string name, int score, bool isLocal) = _entries[index];
                AppendEntry(index + 1, name, score, isLocal);
            }

            if (localRank > visibleCount && localPlayer != null)
            {
                AppendEntry(localRank, localPlayer.DisplayName, Constants.CapacityToScore(localPlayer.Capacity), true);
            }

            _view.ShowLeaderboardText(_builder.ToString());
        }

        private void AppendEntry(int rank, string name, int score, bool highlight)
        {
            _builder.Append('\n');
            if (highlight)
            {
                _builder.Append("<color=").Append(LocalEntryColor).Append('>');
            }

            _builder.Append(rank).Append(". ").Append(name).Append("  ").Append(score.ToString("N0"));

            if (highlight)
            {
                _builder.Append("</color>");
            }
        }

        private void CollectEntries(Player localPlayer)
        {
            _entries.Clear();
            if (_networkManager.SpawnManager == null)
            {
                return;
            }

            foreach (NetworkObject networkObject in _networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject.TryGetComponent(out Player player))
                {
                    if (!player.IsDefeated)
                    {
                        _entries.Add((player.DisplayName, Constants.CapacityToScore(player.Capacity), player == localPlayer));
                    }
                }
                else if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    _entries.Add((enemy.DisplayName, Constants.CapacityToScore(enemy.Capacity), false));
                }
            }
        }
    }
}
