using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PlanetIO
{
    public sealed class EnemySpawner : Spawner<Enemy>, IRespawnService<Enemy>
    {
        [SerializeField, Min(0f)] private float _minimumDistanceFromPlayers = 30f;
        [SerializeField, Min(1)] private int _maxPositionAttempts = 10;

        public void Respawn(Enemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            float minimumScale = MinimumObjectScale;
            enemy.Capacity = minimumScale;

            Vector3 newPosition = GetRespawnPosition();
            enemy.transform.position = newPosition;
            enemy.transform.localScale = new Vector3(minimumScale, minimumScale, 1f);
            enemy.gameObject.SetActive(true);

            if (enemy.TryGetComponent(out NetworkObject networkObject) &&
                networkObject.IsSpawned &&
                enemy.TryGetComponent(out NetworkTransform networkTransform))
            {
                networkTransform.Teleport(
                    newPosition,
                    enemy.transform.rotation,
                    enemy.transform.localScale);
            }
        }

        protected override Vector2 GetRandomPosition()
        {
            if (PlayerRegistry.Count == 0)
            {
                return base.GetRandomPosition();
            }

            for (int i = 0; i < _maxPositionAttempts; i++)
            {
                Vector2 candidate = base.GetRandomPosition();
                if (PlayerRegistry.GetClosestPlayerDistance(candidate) >= _minimumDistanceFromPlayers)
                {
                    return candidate;
                }
            }

            return base.GetRandomPosition();
        }

        private Vector3 GetRespawnPosition()
        {
            for (int i = 0; i < _maxPositionAttempts; i++)
            {
                Vector2 candidate = GetRandomPosition();
                if (PlayerRegistry.GetClosestPlayerDistance(candidate) >= _minimumDistanceFromPlayers)
                {
                    return candidate;
                }
            }

            return (Vector3)GetRandomPosition();
        }
    }
}
