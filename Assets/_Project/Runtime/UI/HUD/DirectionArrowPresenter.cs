using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed class DirectionArrowPresenter : ILateTickable
    {
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private Player _lastPlayer;
        private PlayerDirectionArrow _arrow;

        public DirectionArrowPresenter(
            ILocalPlayerProvider localPlayerProvider)
        {
            _localPlayerProvider = localPlayerProvider ?? throw new System.ArgumentNullException(nameof(localPlayerProvider));
        }

        public void LateTick()
        {
            Player player = _localPlayerProvider.LocalPlayer;

            if (player == null)
            {
                if (_arrow != null) _arrow.Hide();
                return;
            }

            if (_lastPlayer != player)
            {
                _lastPlayer = player;
                _arrow = player.GetComponentInChildren<PlayerDirectionArrow>();
                PlayerMovement movement = player.GetComponent<PlayerMovement>();
                if (_arrow != null && movement != null)
                {
                    _arrow.Bind(movement);
                }
            }

            if (_arrow != null)
            {
                _arrow.Show();
            }
        }
    }
}
