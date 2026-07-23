using UnityEngine.Pool;

namespace Planet_IO
{
    public sealed class CometPool : ObjectPool.ObjectPool<Comet>
    {
        public override void Initialize() =>
            Pool = new ObjectPool<Comet>(OnCreate, OnGet, OnRelease, Destroy, false,
                Count, Count);
    }
}
