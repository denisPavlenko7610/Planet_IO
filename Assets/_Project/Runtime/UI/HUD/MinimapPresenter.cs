using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class MinimapPresenter : ITickable
    {
        private static readonly Color LeaderColor = new(1f, 0.82f, 0.25f);

        private readonly NetworkManager _networkManager;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly List<MinimapBlip> _blips = new();
        private readonly MinimapView _view;

        public MinimapPresenter(
            MinimapView view,
            NetworkManager networkManager,
            ILocalPlayerProvider localPlayerProvider)
        {
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

            _blips.Clear();
            foreach (NetworkObject networkObject in _networkManager.SpawnManager.SpawnedObjectsList)
            {
                Color color;
                bool isLeader;
                if (networkObject.TryGetComponent(out Player player))
                {
                    bool isLocal = player == _localPlayerProvider.LocalPlayer;
                    if (player.IsDefeated || player.IsHiddenFromRivals && !isLocal)
                    {
                        continue;
                    }

                    isLeader = player.IsLeader;
                    color = isLocal
                        ? new Color(0.55f, 1f, 0.5f)
                        : Color.white;
                }
                else if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    isLeader = enemy.IsLeader;
                    color = new Color(0.8f, 0.72f, 1f, 0.9f);
                }
                else
                {
                    continue;
                }

                if (isLeader)
                {
                    color = LeaderColor;
                }

                Vector2 position = networkObject.transform.position;
                Rect bounds = Constants.WorldBounds;
                Vector2 normalized = new(
                    (position.x - bounds.xMin) / bounds.width,
                    (position.y - bounds.yMin) / bounds.height);

                _blips.Add(new MinimapBlip(normalized, color, isLeader));
            }

            _view.UpdateBlips(_blips);
        }
    }
}
