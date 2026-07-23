using Planet_IO.ObjectPool;
using VContainer;

namespace Planet_IO
{
    public sealed class EnemySpawner : Spawner<Enemy>, IRespawnService<Enemy>
    {
        public void CreateEnemy(Enemy enemy)
        {
            RespawnObject(enemy);
        }

        public void Respawn(Enemy enemy) => CreateEnemy(enemy);
    }
}
