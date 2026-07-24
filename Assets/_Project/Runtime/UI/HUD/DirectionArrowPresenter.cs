using PlanetIO.Utils;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class DirectionArrowPresenter : ILateTickable
    {
        private readonly IDirectionArrowView _directionArrowView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private UnityEngine.Camera _camera;
        private Player _lastPlayer;
        private PlayerMovement _playerMovement;
        private float _initialCapacity;

        public DirectionArrowPresenter(
            IDirectionArrowView directionArrowView,
            ILocalPlayerProvider localPlayerProvider)
        {
            _directionArrowView = directionArrowView ?? throw new System.ArgumentNullException(nameof(directionArrowView));
            _localPlayerProvider = localPlayerProvider ?? throw new System.ArgumentNullException(nameof(localPlayerProvider));
        }

        public void LateTick()
        {
            Player player = _localPlayerProvider.LocalPlayer;
            _camera ??= UnityEngine.Camera.main;

            if (player == null || _camera == null)
            {
                _directionArrowView.Hide();
                return;
            }

            BindMovement(player);
            if (_playerMovement == null)
            {
                _directionArrowView.Hide();
                return;
            }

            Vector2 direction = _playerMovement.Direction.normalized;
            if (direction == Vector2.zero)
            {
                direction = player.transform.right;
            }

            float playerRadius = player.Capacity * _directionArrowView.PlayerVisualRadius;
            Vector3 worldPosition = player.transform.position + (Vector3)(direction * (playerRadius + _directionArrowView.DistanceFromPlayer));
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float screenScale = Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f);
            float capacityScale = Mathf.Clamp(Mathf.Sqrt(player.Capacity / _initialCapacity), 0.8f, _directionArrowView.MaximumScale);

            _directionArrowView.Show(_camera.WorldToScreenPoint(worldPosition), angle, screenScale * capacityScale);
        }

        private void BindMovement(Player player)
        {
            if (_lastPlayer == player)
            {
                return;
            }

            _lastPlayer = player;
            _playerMovement = player.GetComponent<PlayerMovement>();
            _initialCapacity = Mathf.Max(player.Capacity, Constants.MinimumDisplayCapacity);
        }
    }
}
