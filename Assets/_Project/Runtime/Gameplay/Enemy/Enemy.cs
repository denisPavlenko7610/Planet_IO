using System;
using PlanetIO.Core.Attributes;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(EnemyMovement))]
    public sealed class Enemy : PlanetScale
    {
        [Header("Presentation")]
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField, Assign] private SpriteRenderer _spriteRenderer;

        [Header("Balance")]
        [SerializeField, Min(0f)] private float _enemyFoodGrowthMultiplier = 0.01f;
        [SerializeField, Min(0f)] private float _enemyCometDamageMultiplier = 0.02f;
        [SerializeField, Min(1f)] private float _eatSizeRatio = 1.08f;

        [Header("Spawn")]
        [SerializeField, Min(0.01f)] private float _initialCapacity = 0.1f;

        private IGameStateService _gameStateService;
        private IRespawnService<Enemy> _enemyRespawnService;
        private bool _servicesReady;

        protected override float FoodGrowthMultiplier => _enemyFoodGrowthMultiplier;
        protected override float CometDamageMultiplier => _enemyCometDamageMultiplier;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Point> pointRespawnService,
            IRespawnService<Enemy> enemyRespawnService,
            IGameStateService gameStateService)
        {
            CometRespawnService = cometRespawnService ?? throw new ArgumentNullException(nameof(cometRespawnService));
            PointRespawnService = pointRespawnService ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _enemyRespawnService = enemyRespawnService ?? throw new ArgumentNullException(nameof(enemyRespawnService));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
            _servicesReady = true;
        }

        protected override void Awake()
        {
            base.Awake();
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
            Capacity = _initialCapacity;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetDeterministicSprite();
        }

        private void OnTriggerEnter2D(Collider2D collider2D)
        {
            if (!IsServer ||
                !_servicesReady ||
                !_gameStateService.IsGameplayActive ||
                collider2D == null)
            {
                return;
            }

            if (collider2D.TryGetComponent(out Enemy otherEnemy))
            {
                if (otherEnemy == this)
                {
                    return;
                }

                if (Capacity >= otherEnemy.Capacity * _eatSizeRatio)
                {
                    Grow(otherEnemy.Capacity);
                    _enemyRespawnService.Respawn(otherEnemy);
                }
            }
            else if (collider2D.TryGetComponent(out Player player))
            {
                if (!player.IsDefeated &&
                    Capacity >= player.Capacity * _eatSizeRatio)
                {
                    Grow(player.Capacity);
                    player.Defeat();
                }
            }
            else
            {
                HandleEntityCollision(collider2D);
            }
        }

        private void SetDeterministicSprite()
        {
            if (_enemyConfig == null || _spriteRenderer == null)
            {
                return;
            }

            Sprite sprite = _enemyConfig.GetSprite(NetworkObjectId);
            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
    }
}
