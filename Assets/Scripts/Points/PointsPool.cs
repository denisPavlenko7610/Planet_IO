using UnityEngine.Pool;

namespace PlanetIO
{
    public sealed class PointsPool : Pool.ObjectPool<Point>
    {
        public override void Init() =>
            Pool = new ObjectPool<Point>(OnCreate, OnGet, OnRelease, OnDestroy, false,
                Count, Count + Count);
    }
}