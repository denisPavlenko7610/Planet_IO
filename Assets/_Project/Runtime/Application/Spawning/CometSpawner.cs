using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    public sealed class CometSpawner : Spawner<Comet>, IRespawnService<Comet>
    {
        [SerializeField] private BordersTrigger _bordersTrigger;

        private NetworkManager _networkManager;

        [Inject]
        public void Construct(NetworkManager networkManager)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
        }

        private void OnEnable()
        {
            _bordersTrigger.CometTriggered += Respawn;
        }

        private void OnDisable()
        {
            _bordersTrigger.CometTriggered -= Respawn;
        }

        public void Respawn(Comet comet)
        {
            if (_networkManager?.IsServer == true)
            {
                RespawnObject(comet);
            }
        }
    }
}
