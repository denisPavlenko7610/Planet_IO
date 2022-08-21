using Planet_IO.ObjectPool;
using UnityEngine;
using Zenject;

namespace Planet_IO
{
    public class CometsSpawnerLogic : Spawner<Comet>
    {
        [SerializeField] private BordersTrigger _bordersTrigger;
        
        private ObjectPool<Comet> _cometsPool;
        private Spawner<Comet> _cometSpawner;

        [Inject]
        private void Construct(Spawner<Comet> cometSpawner, ObjectPool<Comet> cometsObjectPool)
        {
            _cometSpawner = cometSpawner;
            _cometsPool = cometsObjectPool;
        }

        private void OnEnable() => _bordersTrigger.OnCometTriggeredHandler += CreateComet;
        private void OnDisable() => _bordersTrigger.OnCometTriggeredHandler -= CreateComet;

        public void CreateComet(Comet comet)
        {
            _cometsPool.Pool.Release(comet);
            _cometSpawner.CreateObject();
        }
    }
}