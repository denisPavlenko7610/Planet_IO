using UnityEngine.Pool;

namespace Planet_IO
{
    public class EnemyPool : ObjectPool.ObjectPool<Enemy>
    {
        public override void Init() =>
            Pool = new ObjectPool<Enemy>(OnCreate, OnGet, OnRelease, Destroy, false,
                Count, Count);

    }
}