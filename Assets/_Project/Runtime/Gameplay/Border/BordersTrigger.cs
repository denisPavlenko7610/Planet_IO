using System;
using UnityEngine;

namespace PlanetIO
{
    public sealed class BordersTrigger : MonoBehaviour
    {
        public event Action<Comet> CometTriggered;
        public event Action<Player> PlayerTriggered;

        private void OnTriggerEnter2D(Collider2D otherCollider)
        {
            if (otherCollider.TryGetComponent(out Comet comet))
            {
                CometTriggered?.Invoke(comet);
            }
            else if (otherCollider.TryGetComponent(out Player player))
            {
                PlayerTriggered?.Invoke(player);
            }
        }
    }
}
