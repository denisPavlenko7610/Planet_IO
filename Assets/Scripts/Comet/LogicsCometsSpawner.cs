using Pool;
using Zenject;
using Spawner;
using UnityEngine;

namespace PlanetIO
{
    public class LogicsCometsSpawner : Spawner<Comet>
    {
        [SerializeField] private BordersTrigger _bordersTrigger;
        private ObjectPool<Comet> _cometsObjectPool;
        private Spawner<Comet> _cometSpawner;

        [Inject]
        private void Construct(Spawner<Comet> cometSpawner, ObjectPool<Comet> cometsObjectPool)
        {
            _cometSpawner = cometSpawner;
            _cometsObjectPool = cometsObjectPool;
        }

        private void OnEnable() => _bordersTrigger.OnCometTriggeredHandler += CreateComet;
        private void OnDisable() => _bordersTrigger.OnCometTriggeredHandler -= CreateComet;

        private void CreateComet(Comet comet)
        {
            _cometsObjectPool.Pool.Release(comet);
            _cometSpawner.CreateObject();
        }
    }
}