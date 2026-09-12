using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class MinimapPresenter : IStartable, ITickable
    {
        private readonly NetworkManager _networkManager;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly List<MinimapBlip> _blips = new();
        private MinimapView _view;

        public MinimapPresenter(
            NetworkManager networkManager,
            ILocalPlayerProvider localPlayerProvider)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
        }

        public void Start()
        {
            Canvas overlayCanvas = null;
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    overlayCanvas = canvas;
                    break;
                }
            }

            if (overlayCanvas != null)
            {
                _view = overlayCanvas.gameObject.AddComponent<MinimapView>();
            }
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
                if (networkObject.TryGetComponent(out Player player))
                {
                    if (player.IsDefeated)
                    {
                        continue;
                    }

                    color = player == _localPlayerProvider.LocalPlayer
                        ? new Color(0.55f, 1f, 0.5f)
                        : Color.white;
                }
                else if (networkObject.TryGetComponent(out Enemy enemy))
                {
                    color = new Color(1f, 1f, 1f, 0.45f);
                }
                else
                {
                    continue;
                }

                Vector2 position = networkObject.transform.position;
                Rect bounds = Constants.WorldBounds;
                Vector2 normalized = new(
                    (position.x - bounds.xMin) / bounds.width,
                    (position.y - bounds.yMin) / bounds.height);

                _blips.Add(new MinimapBlip(normalized, color));
            }

            _view.UpdateBlips(_blips);
        }
    }
}
