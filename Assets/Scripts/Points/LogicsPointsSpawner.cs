using Planet_IO.ObjectPool;
using Zenject;

namespace Planet_IO
{
    public sealed class LogicsPointsSpawner : Spawner<Point>
    {
        private ObjectPool<Point> _pointPool;
        private Spawner<Point> _pointSpawner;

        [Inject]
        private void Construct(ObjectPool<Point> pointPool, Spawner<Point> pointSpawner)
        {
            _pointPool = pointPool;
            _pointSpawner = pointSpawner;
        }

        public void CreatePoint(Point point)
        {
            _pointPool.Pool.Release(point);
            _pointSpawner.CreateObject();
        }
    }
}