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

        public LocalPlayerProvider(NetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
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

            return playerObject != null && playerObject.IsSpawned && playerObject.TryGetComponent(out Player player)
                ? player
                : null;
        }
    }
}
