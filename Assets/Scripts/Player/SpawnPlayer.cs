using PlanetIO_Core;
using Unity.Netcode;
using UnityEngine;

namespace Planet_IO
{
    public class SpawnPlayer : NetworkBehaviour
    {
        [SerializeField] private SceneInstaller _sceneInstaller;
        [SerializeField] private float _spawnRange = 50f;
        
        public override void OnNetworkSpawn()
        {
            transform.position = new Vector3(Random.Range(_spawnRange, -_spawnRange),
                Random.Range(_spawnRange, -_spawnRange), 0);
            
            _sceneInstaller.Install();
        }
    }
}