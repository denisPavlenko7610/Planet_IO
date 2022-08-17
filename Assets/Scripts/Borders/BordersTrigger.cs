using System;
using UnityEngine;

namespace PlanetIO
{
    public class BordersTrigger : MonoBehaviour
    {
        public event Action<Comet> OnCometTriggeredHandler;

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out Comet comet))
            {
                OnCometTriggeredHandler?.Invoke(comet);
            }
        }
    }
    
}

