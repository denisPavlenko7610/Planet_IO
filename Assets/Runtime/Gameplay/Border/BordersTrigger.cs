using System;
using UnityEngine;

namespace Planet_IO
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
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.TryGetComponent(out Player player))
            {
                PlayerTriggered?.Invoke(player);
            }
        }
    }
}
