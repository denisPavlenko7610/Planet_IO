using UnityEngine.Pool;

namespace PlanetIO
{
    public class CometsPool : Pool.ObjectPool<Comet>
    {
        public override void Init() =>
            Pool = new ObjectPool<Comet>(OnCreate, OnGet, OnRelease, OnDestroy, false,
                Count, Count);
    }
}