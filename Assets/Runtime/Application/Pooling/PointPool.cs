using UnityEngine.Pool;

namespace Planet_IO
{
    public sealed class PointPool : ObjectPool.ObjectPool<Point>
    {
        public override void Initialize() =>
            Pool = new ObjectPool<Point>(OnCreate, OnGet, OnRelease, Destroy, false,
                Count, Count + Count);
    }
}
