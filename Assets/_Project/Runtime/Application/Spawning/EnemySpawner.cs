using System;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    public sealed class EnemySpawner : Spawner<Enemy>, IRespawnService<Enemy>
    {
        [SerializeField, Min(0f)] private float _minimumDistanceFromPlayers = 30f;
        [SerializeField, Min(1)] private int _maxPositionAttempts = 10;

        private ILootSpawnService _lootSpawnService;
        private IPlayerRegistry _playerRegistry;

        [Inject]
        public void Construct(ILootSpawnService lootSpawnService, IPlayerRegistry playerRegistry)
        {
            _lootSpawnService = lootSpawnService ?? throw new ArgumentNullException(nameof(lootSpawnService));
            _playerRegistry = playerRegistry ?? throw new ArgumentNullException(nameof(playerRegistry));
        }

        public void Respawn(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            enemy.PlayDeathEffect();
            _lootSpawnService?.SpawnLoot(enemy.transform.position, enemy.Capacity);

            enemy.Capacity = enemy.InitialCapacity;

            Vector3 newPosition = GetRandomPosition();
            enemy.transform.position = newPosition;
            enemy.gameObject.SetActive(true);

            if (enemy.TryGetComponent(out EnemyMovement movement))
            {
                movement.ResetForRespawn();
            }

            TeleportIfSpawned(enemy);
        }

        protected override Vector2 GetRandomPosition()
        {
            if (_playerRegistry.Count == 0)
            {
                return base.GetRandomPosition();
            }

            for (int i = 0; i < _maxPositionAttempts; i++)
            {
                Vector2 candidate = base.GetRandomPosition();
                if (!_playerRegistry.IsAnyPlayerWithinDistance(candidate, _minimumDistanceFromPlayers))
                {
                    return candidate;
                }
            }

            return base.GetRandomPosition();
        }
    }
}
