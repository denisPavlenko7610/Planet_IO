using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class MinimapPresenter : ITickable
    {
        private const float RefreshIntervalSeconds = 0.1f;

        private static readonly Color LeaderColor = new(1f, 0.82f, 0.25f);
        private static readonly Color LocalColor = new(0.55f, 1f, 0.5f);
        private static readonly Color BotColor = new(0.8f, 0.72f, 1f, 0.9f);

        private readonly NetworkManager _networkManager;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly List<MinimapBlip> _blips = new();
        private readonly MinimapView _view;
        private readonly IWorldEvents _events;
        private float _refreshTimeRemaining;

        public MinimapPresenter(
            MinimapView view,
            NetworkManager networkManager,
            ILocalPlayerProvider localPlayerProvider,
            IWorldEvents events)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
        }

        public void Tick()
        {
            if (_view == null || _networkManager.SpawnManager == null)
            {
                return;
            }

            _refreshTimeRemaining -= Time.unscaledDeltaTime;
            if (_refreshTimeRemaining > 0f)
            {
                return;
            }

            _refreshTimeRemaining = RefreshIntervalSeconds;
            _blips.Clear();
            Player localPlayer = _localPlayerProvider.LocalPlayer;
			Rect bounds = WorldBounds.Area;
			MinimapBlip? localBlip = null;
			IReadOnlyList<PlanetBody> planets = PlanetDirectory.All;
            for (int index = 0; index < planets.Count; index++)
            {
				PlanetBody planet = planets[index];
                if (planet == null || !planet.IsSpawned)
                {
                    continue;
                }

                Color color;
                if (planet is Player player)
                {
                    bool isLocal = player == localPlayer;
                    if (player.IsDefeated || player.IsHiddenFromRivals && !isLocal)
                    {
                        continue;
                    }

                    color = isLocal ? LocalColor : Color.white;
                }
                else if (planet is Enemy)
                {
                    color = BotColor;
                }
                else
                {
                    continue;
                }

                bool isLeader = planet.IsLeader;
                if (isLeader)
                {
                    color = LeaderColor;
                }

                Vector2 position = planet.transform.position;
                Vector2 normalized = new(
                    (position.x - bounds.xMin) / bounds.width,
                    (position.y - bounds.yMin) / bounds.height);

				bool isLocalPlayer = planet == localPlayer;
				var blip = new MinimapBlip(normalized, color, isLeader, isLocalPlayer);
				if (isLocalPlayer) localBlip = blip;
				else _blips.Add(blip);
            }

			AddEventMarker();
			if (localBlip.HasValue) _blips.Add(localBlip.Value);
            _view.UpdateBlips(_blips);
        }

        private void AddEventMarker()
        {
            if (!_events.HasMarker)
            {
                return;
            }

            Vector2 position = _events.MarkerPosition;
			Rect bounds = WorldBounds.Area;
            Vector2 normalized = new(
                Mathf.Clamp01((position.x - bounds.xMin) / bounds.width),
                Mathf.Clamp01((position.y - bounds.yMin) / bounds.height));
            Color color = _events.MarkerType switch
            {
                CosmicEventType.MeteorShower => new Color(1f, 0.55f, 0.2f),
                CosmicEventType.Supernova => new Color(1f, 0.95f, 0.4f),
                _ => new Color(0.8f, 0.45f, 1f)
            };

			_blips.Add(new MinimapBlip(normalized, color, true, false, true));
        }
    }
}
