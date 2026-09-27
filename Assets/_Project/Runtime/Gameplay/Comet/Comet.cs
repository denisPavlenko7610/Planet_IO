using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed class Comet : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }

        public void PlayImpactEffect()
        {
            if (IsServer && IsSpawned)
            {
                PlayImpactEffectRpc(transform.position, transform.localScale.x);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PlayImpactEffectRpc(Vector2 position, float size)
        {
            GameVfx.Impact(position, size);
        }
    }
}
