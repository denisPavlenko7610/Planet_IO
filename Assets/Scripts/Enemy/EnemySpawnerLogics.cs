using Planet_IO.ObjectPool;
using Zenject;

namespace Planet_IO
{
    public class EnemySpawnerLogics : Spawner<EnemyScale>
    {
        private ObjectPool<EnemyScale> _enemyPool;
        private Spawner<EnemyScale> _enemySpawner;


        [Inject]
        private void Construct(ObjectPool<EnemyScale> enemyPool, Spawner<EnemyScale> enemySpawner)
        {
            _enemyPool = enemyPool;
            _enemySpawner = enemySpawner;
        }

        public void CreateEnemy(EnemyScale enemy)
        {
            _enemyPool.Pool.Release(enemy);
            _enemySpawner.CreateObject();
        }
    }
}