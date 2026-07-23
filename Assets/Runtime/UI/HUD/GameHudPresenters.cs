using System;
using Planet_IO;
using Planet_IO.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public interface ILocalPlayerProvider
    {
        event Action<Player> LocalPlayerChanged;

        Player LocalPlayer { get; }
    }

    public sealed class LocalPlayerProvider :
        ILocalPlayerProvider,
        ITickable,
        IDisposable
    {
        private readonly NetworkManager _networkManager;

        public LocalPlayerProvider(NetworkManager networkManager)
        {
            _networkManager = networkManager
                ?? throw new ArgumentNullException(nameof(networkManager));
        }

        public event Action<Player> LocalPlayerChanged;

        public Player LocalPlayer { get; private set; }

        public void Tick()
        {
            Player currentPlayer = GetCurrentLocalPlayer();
            if (currentPlayer == LocalPlayer)
            {
                return;
            }

            LocalPlayer = currentPlayer;
            LocalPlayerChanged?.Invoke(LocalPlayer);
        }

        public void Dispose()
        {
            LocalPlayer = null;
            LocalPlayerChanged = null;
        }

        private Player GetCurrentLocalPlayer()
        {
            NetworkObject playerObject =
                _networkManager.LocalClient?.PlayerObject;

            return playerObject != null &&
                   playerObject.IsSpawned &&
                   playerObject.TryGetComponent(out Player player)
                ? player
                : null;
        }
    }

    public sealed class ScorePresenter : IStartable, IDisposable
    {
        private readonly IScoreView _scoreView;
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private Player _boundPlayer;

        public ScorePresenter(
            IScoreView scoreView,
            ILocalPlayerProvider localPlayerProvider)
        {
            _scoreView = scoreView
                ?? throw new ArgumentNullException(nameof(scoreView));
            _localPlayerProvider = localPlayerProvider
                ?? throw new ArgumentNullException(nameof(localPlayerProvider));
        }

        public void Start()
        {
            _localPlayerProvider.LocalPlayerChanged += OnLocalPlayerChanged;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _localPlayerProvider.LocalPlayerChanged -= OnLocalPlayerChanged;
            BindPlayer(null);
        }

        private void OnLocalPlayerChanged(Player player)
        {
            BindPlayer(player);
        }

        private void BindPlayer(Player player)
        {
            if (_boundPlayer != null)
            {
                _boundPlayer.CapacityChanged -= OnCapacityChanged;
            }

            _boundPlayer = player;
            if (_boundPlayer == null)
            {
                _scoreView.ShowScore(0);
                return;
            }

            _boundPlayer.CapacityChanged += OnCapacityChanged;
            OnCapacityChanged(_boundPlayer.Capacity);
        }

        private void OnCapacityChanged(float capacity)
        {
            int score = Mathf.RoundToInt(capacity * Constants.ScaleMultiplier);
            _scoreView.ShowScore(score);
        }
    }

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
            _directionArrowView = directionArrowView
                ?? throw new ArgumentNullException(nameof(directionArrowView));
            _localPlayerProvider = localPlayerProvider
                ?? throw new ArgumentNullException(nameof(localPlayerProvider));
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

            float playerRadius =
                player.Capacity * _directionArrowView.PlayerVisualRadius;
            Vector3 worldPosition =
                player.transform.position +
                (Vector3)(
                    direction *
                    (playerRadius + _directionArrowView.DistanceFromPlayer));
            float angle =
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float screenScale =
                Mathf.Clamp(Screen.height / 1080f, 0.75f, 1.5f);
            float capacityScale = Mathf.Clamp(
                Mathf.Sqrt(player.Capacity / _initialCapacity),
                0.8f,
                _directionArrowView.MaximumScale);

            _directionArrowView.Show(
                _camera.WorldToScreenPoint(worldPosition),
                angle,
                screenScale * capacityScale);
        }

        private void BindMovement(Player player)
        {
            if (_lastPlayer == player)
            {
                return;
            }

            _lastPlayer = player;
            _playerMovement = player.GetComponent<PlayerMovement>();
            _initialCapacity = Mathf.Max(player.Capacity, 0.01f);
        }
    }
}
