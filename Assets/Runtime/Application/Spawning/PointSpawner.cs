using Planet_IO.ObjectPool;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    public sealed class PointSpawner : Spawner<Point>, IRespawnService<Point>, ISpawnService<Point>
    {
        public void CreatePoint(Point point)
        {
            RespawnObject(point);
        }
        
        public void CreatePoint(Transform pos) => CreateObject(pos);

        public void Respawn(Point point) => CreatePoint(point);

        public void SpawnAt(Transform position) => CreatePoint(position);
    }
}
