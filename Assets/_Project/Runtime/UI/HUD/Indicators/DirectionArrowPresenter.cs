using System;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class DirectionArrowPresenter : IStartable, IDisposable
    {
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private PlayerDirectionArrow _arrow;

        public DirectionArrowPresenter(
            ILocalPlayerProvider localPlayerProvider)
        {
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
        }

        public void Start()
        {
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _localPlayerProvider.LocalPlayerChanged -= BindPlayer;
            BindPlayer(null);
        }

        private void BindPlayer(Player player)
        {
            if (_arrow != null)
            {
                _arrow.Hide();
            }

            _arrow = player != null
                ? player.GetComponentInChildren<PlayerDirectionArrow>()
                : null;

            if (_arrow == null ||
                !player.TryGetComponent(out PlayerMovement movement))
            {
                return;
            }

            _arrow.Bind(movement);
            _arrow.Show();
        }
    }
}
