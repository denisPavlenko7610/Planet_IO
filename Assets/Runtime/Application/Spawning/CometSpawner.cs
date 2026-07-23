using Planet_IO.ObjectPool;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    public sealed class CometSpawner : Spawner<Comet>, IRespawnService<Comet>
    {
        [SerializeField] private BordersTrigger _bordersTrigger;
        
        private void OnEnable() => _bordersTrigger.OnCometTriggeredHandler += CreateComet;
        private void OnDisable() => _bordersTrigger.OnCometTriggeredHandler -= CreateComet;

        public void CreateComet(Comet comet)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            RespawnObject(comet);
        }

        public void Respawn(Comet comet) => CreateComet(comet);
    }
}
