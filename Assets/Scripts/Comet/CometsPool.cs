using UnityEngine.Pool;

namespace Planet_IO
{
    public class CometsPool :  ObjectPool.ObjectPool<Comet>
    {
        public override void Init() =>
            Pool = new ObjectPool<Comet>(OnCreate, OnGet, OnRelease, OnDestroy, false,
                Count, Count);
    }
}