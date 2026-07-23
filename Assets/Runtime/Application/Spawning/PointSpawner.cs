using UnityEngine;

namespace Planet_IO
{
    public sealed class PointSpawner :
        Spawner<Point>,
        IRespawnService<Point>,
        ISpawnService<Point>
    {
        public void Respawn(Point point) => RespawnObject(point);

        public void SpawnAt(Transform spawnTransform) =>
            CreateObject(spawnTransform);
    }
}
