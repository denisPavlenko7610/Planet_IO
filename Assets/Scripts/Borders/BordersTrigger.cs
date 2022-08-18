using System;
using UnityEngine;

namespace PlanetIO
{
    public class BordersTrigger : MonoBehaviour
    {
        public event Action<Comet> OnCometTriggeredHandler;
        public event Action<float> OnPlayerTriggeredHandler;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out Comet comet)) 
                OnCometTriggeredHandler?.Invoke(comet);
         
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (col.collider.TryGetComponent(out PlayerScale player))
                OnPlayerTriggeredHandler?.Invoke(player._capacityPlayer);
        }
    }
    
}

