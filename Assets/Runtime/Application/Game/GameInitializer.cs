using Planet_IO.ObjectPool;
using Unity.Netcode;
using VContainer.Unity;

namespace Planet_IO.Application
{
    public sealed class GameInitializer : IStartable
    {
        private readonly PointSpawner _pointSpawner;
        private readonly CometSpawner _cometSpawner;
        private readonly EnemySpawner _enemySpawner;
        private readonly ObjectPool<Point> _pointsPool;
        private readonly ObjectPool<Comet> _cometsPool;
        private readonly ObjectPool<Enemy> _enemyPool;
        private readonly NetworkManager _networkManager;

        public GameInitializer(
            PointSpawner pointSpawner,
            CometSpawner cometSpawner,
            EnemySpawner enemySpawner,
            ObjectPool<Point> pointsPool,
            ObjectPool<Comet> cometsPool,
            ObjectPool<Enemy> enemyPool,
            NetworkManager networkManager)
        {
            _pointSpawner = pointSpawner;
            _cometSpawner = cometSpawner;
            _enemySpawner = enemySpawner;
            _pointsPool = pointsPool;
            _cometsPool = cometsPool;
            _enemyPool = enemyPool;
            _networkManager = networkManager;
        }

        public void Start()
        {
            if (!_networkManager.IsServer)
            {
                return;
            }

            _pointSpawner.Initialize(_pointsPool);
            _cometSpawner.Initialize(_cometsPool);
            _enemySpawner.Initialize(_enemyPool);
        }
    }
}
