using System;
using Unity.Netcode;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public interface ILocalPlayerProvider
    {
        event Action<Player> LocalPlayerChanged;

        Player LocalPlayer { get; }
    }

    public sealed class LocalPlayerProvider : ILocalPlayerProvider, ITickable, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private NetworkObject _localPlayerObject;

        public LocalPlayerProvider(NetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        }

        public event Action<Player> LocalPlayerChanged;

        public Player LocalPlayer { get; private set; }

        public void Tick()
        {
            NetworkObject playerObject =
                _networkManager.LocalClient?.PlayerObject;
            bool isAvailable = playerObject != null &&
                               playerObject.IsSpawned;

            if (playerObject == _localPlayerObject &&
                isAvailable == (LocalPlayer != null))
            {
                return;
            }

            _localPlayerObject = playerObject;
            Player currentPlayer = isAvailable &&
                                   playerObject.TryGetComponent(out Player player)
                ? player
                : null;

            if (currentPlayer == LocalPlayer)
            {
                return;
            }

            LocalPlayer = currentPlayer;
            LocalPlayerChanged?.Invoke(LocalPlayer);
        }

        public void Dispose()
        {
            _localPlayerObject = null;
            LocalPlayer = null;
            LocalPlayerChanged = null;
        }
    }
}
