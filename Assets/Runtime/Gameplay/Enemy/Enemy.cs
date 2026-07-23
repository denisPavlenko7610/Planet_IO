using System;
using PlanetIO.Core.Attributes;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(EnemyMovement))]
    public sealed class Enemy : PlanetScale
    {
        [Header("Presentation")]
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField, Assign] private SpriteRenderer _spriteRenderer;

        [Header("Balance")]
        [SerializeField, Min(0f)] private float _foodGrowthMultiplier = 0.01f;
        [SerializeField, Min(0f)] private float _cometDamageMultiplier = 0.02f;

        private IRespawnService<Comet> _cometRespawnService;
        private IRespawnService<Point> _pointRespawnService;
        private IGameStateService _gameStateService;
        private bool _servicesReady;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Point> pointRespawnService,
            IGameStateService gameStateService)
        {
            _cometRespawnService = cometRespawnService
                ?? throw new ArgumentNullException(nameof(cometRespawnService));
            _pointRespawnService = pointRespawnService
                ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _gameStateService = gameStateService
                ?? throw new ArgumentNullException(nameof(gameStateService));
            _servicesReady = true;
        }

        protected override void Awake()
        {
            base.Awake();
            _spriteRenderer ??= GetComponent<SpriteRenderer>();
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

            if (collider2D.TryGetComponent(out Point point))
            {
                Grow(point.Capacity * _foodGrowthMultiplier);
                _pointRespawnService.Respawn(point);
            }
            else if (collider2D.TryGetComponent(out Comet comet))
            {
                Shrink(comet.Capacity * _cometDamageMultiplier);
                _cometRespawnService.Respawn(comet);
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
