using UnityEngine.Pool;

namespace Planet_IO
{
    public class EnemyPool : ObjectPool.ObjectPool<EnemyScale>
    {
        public override void Init() =>
            Pool = new ObjectPool<EnemyScale>(OnCreate, OnGet, OnRelease, Destroy, false,
                Count, Count);

    }
}