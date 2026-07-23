using PlanetIO.Core.Attributes;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    public class Enemy : PlanetScale
    {
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField, Assign] private SpriteRenderer _spriteRenderer;

        private IRespawnService<Comet> _cometRespawnService;
        private IRespawnService<Point> _pointRespawnService;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Point> pointRespawnService)
        {
            _cometRespawnService = cometRespawnService;
            _pointRespawnService = pointRespawnService;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetDeterministicSprite();
        }

        private void OnTriggerEnter2D(Collider2D collider2D)
        {
            if (!IsServer)
            {
                return;
            }

            if (collider2D.TryGetComponent(out Point point))
            {
                Grow(point.Capacity * 0.01f);
                _pointRespawnService.Respawn(point);
            }
            else if (collider2D.TryGetComponent(out Comet comet))
            {
                Shrink(comet.Capacity * 0.02f);
                _cometRespawnService.Respawn(comet);
            }
        }

        private void SetDeterministicSprite()
        {
            if (_enemyConfig == null ||
                _enemyConfig.EnemySprites == null ||
                _enemyConfig.EnemySprites.Count == 0 ||
                _spriteRenderer == null)
            {
                return;
            }

            int index = (int)(NetworkObjectId % (ulong)_enemyConfig.EnemySprites.Count);
            _spriteRenderer.sprite = _enemyConfig.EnemySprites[index];
        }
    }
}
