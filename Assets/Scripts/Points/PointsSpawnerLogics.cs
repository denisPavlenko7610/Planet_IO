using Planet_IO.ObjectPool;
using UnityEngine;
using Zenject;

namespace Planet_IO
{
    public class PointsSpawnerLogics : Spawner<Point>
    {
        private ObjectPool<Point> _pointPool;
        private Spawner<Point> _pointSpawner;

        [Inject]
        private void Construct(ObjectPool<Point> pointPool, Spawner<Point> pointSpawner)
        {
            _pointPool = pointPool;
            _pointSpawner = pointSpawner;
        }

        public virtual void CreatePoint(Point point)
        {
            _pointPool.Pool.Release(point);
            _pointSpawner.CreateObject();
        }
        
        public void CreatePoint(Transform pos) => _pointSpawner.CreateObject(pos);
    }
}