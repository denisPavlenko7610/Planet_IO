using System;
using RD.Settings;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class BlitzDirector : IBlitzMatch, IStartable, ITickable, IDisposable
    {
        private readonly ISettingsService _settings;
        private readonly INetworkSessionService _session;
        private readonly IGameStateService _gameState;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly IWorldHudView _view;
        private float _elapsed;
        private int _shownSecond = -1;

        public BlitzDirector(
            ISettingsService settings,
            INetworkSessionService session,
            IGameStateService gameState,
            ILocalPlayerProvider localPlayerProvider,
            IWorldHudView view)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public event Action Finished;

        public bool IsEnabled { get; private set; }
        public bool IsFinished { get; private set; }
        public int FinalRank { get; private set; }
        public int FinalCount { get; private set; }

        public void Start()
        {
            IsEnabled = _settings.Get(GameSettingKeys.BlitzMode) && _session.Mode == NetworkSessionMode.SinglePlayer;
            if (IsEnabled)
            {
                ShowClock(BlitzRules.DurationSeconds);
            }
        }

        public void Dispose()
        {
            _view.SetBlitzTimer(string.Empty, false);
        }

        public void Restart()
        {
            IsFinished = false;
            _elapsed = 0f;
            _shownSecond = -1;
            if (IsEnabled)
            {
                ShowClock(BlitzRules.DurationSeconds);
            }
        }

        public void Tick()
        {
            if (!IsEnabled || IsFinished || !_gameState.IsGameplayActive)
            {
                return;
            }

            _elapsed += Time.unscaledDeltaTime;
            ShowClock(BlitzRules.Remaining(_elapsed));

            if (BlitzRules.IsOver(_elapsed))
            {
                Finish();
            }
        }

        private void ShowClock(float remaining)
        {
            int second = Mathf.CeilToInt(remaining);
            if (second == _shownSecond)
            {
                return;
            }

            _shownSecond = second;
            _view.SetBlitzTimer(BlitzRules.FormatClock(remaining), true);
        }

        private void Finish()
        {
            Player player = _localPlayerProvider.LocalPlayer;
            var capacities = new System.Collections.Generic.List<float>();
            var planets = PlanetDirectory.All;
            for (int index = 0; index < planets.Count; index++)
            {
                PlanetScale planet = planets[index];
                if (planet != null && planet.isActiveAndEnabled && planet.IsSpawned && planet.AreMoonsVisible)
                {
                    capacities.Add(planet.Capacity);
                }
            }

            float own = player != null ? player.Capacity : 0f;
            if (player != null && player.IsDefeated)
            {
                capacities.Add(own);
            }

            FinalCount = Mathf.Max(1, capacities.Count);
            FinalRank = Mathf.Min(FinalCount, BlitzRules.RankOf(own, capacities.ToArray()));
            IsFinished = true;
            Finished?.Invoke();

            if (player != null && !player.IsDefeated)
            {
                player.Defeat();
            }
        }
    }
}
