using Pool;
using Zenject;
using Spawner;

namespace PlanetIO
{
    public class LogicsCometsSpawner : Spawner<Comet>
    {
        private ObjectPool<Comet> _cometsObjectPool;
        private Spawner<Comet> _cometSpawner;

        [Inject]
        private void Construct(Spawner<Comet> cometSpawner, ObjectPool<Comet> cometsObjectPool)
        {
            _cometSpawner = cometSpawner;
            _cometsObjectPool = cometsObjectPool;
        }

        public void CreateComet(Comet comet)
        {
            _cometsObjectPool.Pool.Release(comet);
            _cometSpawner.CreateObject();
        }
    }
}