using Planet_IO.ObjectPool;
using Zenject;

namespace Planet_IO
{
    public class EnemySpawnerLogics : Spawner<Enemy>
    {
        private ObjectPool<Enemy> _enemyPool;
        private Spawner<Enemy> _enemySpawner;
        
        [Inject]
        private void Construct(ObjectPool<Enemy> enemyPool, Spawner<Enemy> enemySpawner)
        {
            _enemyPool = enemyPool;
            _enemySpawner = enemySpawner;
        }

        public void CreateEnemy(Enemy enemy)
        {
            _enemyPool.Pool.Release(enemy);
            _enemySpawner.CreateObject();
        }
    }
}