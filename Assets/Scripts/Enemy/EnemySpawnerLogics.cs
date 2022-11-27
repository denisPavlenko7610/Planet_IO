using System;
using Planet_IO.ObjectPool;
using UnityEngine;
using Zenject;

namespace Planet_IO
{
    public class EnemySpawnerLogics : Spawner<Enemy>
    {
        private ObjectPool<Enemy> _enemyPool;
        private Spawner<Enemy> _enemySpawner;

        private CometsSpawnerLogics _cometsSpawnerLogics;
        private PointsSpawnerLogics _pointsSpawnerLogics;
        
        [Inject]
        private void Construct(ObjectPool<Enemy> enemyPool, Spawner<Enemy> enemySpawner,
            CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _enemyPool = enemyPool;
            _enemySpawner = enemySpawner;
        }

        private void OnDisable()
        {
            _enemySpawner.OnObjectCreated -= InitDependencies;
        }

        public void CreateEnemy(Enemy enemy)
        {
            _enemyPool.Pool.Release(enemy);
            _enemySpawner.CreateObject();
            _enemySpawner.OnObjectCreated += InitDependencies;
        }

        private void InitDependencies(Enemy enemy)
        {
            enemy.InitDependencies(_cometsSpawnerLogics, _pointsSpawnerLogics);
        }
    }
}