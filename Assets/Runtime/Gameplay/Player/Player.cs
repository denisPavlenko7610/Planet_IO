using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class Player : PlanetScale
    {
        private BordersTrigger _bordersTrigger;
        private Rigidbody2D _rigidbody2D;

        [Header("Mass balance")]
        [SerializeField, Min(0.001f)] private float _foodGrowthMultiplier = 0.015f;
        [SerializeField, Min(0.001f)] private float _cometDamageMultiplier = 0.02f;
        [SerializeField, Min(0.001f)] private float _boostMassCost = 0.004f;
        [SerializeField, Range(0f, 0.5f)] private float _borderMassLoss = 0.08f;
        [SerializeField, Min(1f)] private float _eatEnemySizeRatio = 1.08f;

        [Header("Boost food")]
        [SerializeField] private Transform _pointSpawnTransform;

        private IRespawnService<Comet> _cometRespawnService;
        private IRespawnService<Point> _pointRespawnService;
        private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<Point> _pointSpawnService;
        private INetworkSessionService _networkSessionService;
        private IGameStateService _gameStateService;
        private bool _servicesReady;
        private bool _borderEventSubscribed;
        private bool _isDefeated;

        public bool CanBoost => Capacity > MinimumCapacity + _boostMassCost;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Point> pointRespawnService,
            IRespawnService<Enemy> enemyRespawnService,
            ISpawnService<Point> pointSpawnService,
            INetworkSessionService networkSessionService,
            IGameStateService gameStateService,
            BordersTrigger bordersTrigger)
        {
            _cometRespawnService = cometRespawnService
                ?? throw new ArgumentNullException(nameof(cometRespawnService));
            _pointRespawnService = pointRespawnService
                ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _enemyRespawnService = enemyRespawnService
                ?? throw new ArgumentNullException(nameof(enemyRespawnService));
            _pointSpawnService = pointSpawnService
                ?? throw new ArgumentNullException(nameof(pointSpawnService));
            _networkSessionService = networkSessionService
                ?? throw new ArgumentNullException(nameof(networkSessionService));
            _gameStateService = gameStateService
                ?? throw new ArgumentNullException(nameof(gameStateService));
            SetBordersTrigger(
                bordersTrigger
                ?? throw new ArgumentNullException(nameof(bordersTrigger)));
            _servicesReady = true;
        }

        protected override void Awake()
        {
            base.Awake();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            SubscribeToBorderEvent();
        }

        private void OnDisable()
        {
            UnsubscribeFromBorderEvent();
        }

        public void EnableBoost()
        {
            if (!IsOwner || _gameStateService?.IsGameplayActive != true)
            {
                return;
            }

            ApplyBoostRpc();
        }

        [Rpc(SendTo.Server)]
        private void ApplyBoostRpc()
        {
            if (!_servicesReady ||
                !_gameStateService.IsGameplayActive ||
                Capacity <= MinimumCapacity + _boostMassCost)
            {
                return;
            }

            if (Shrink(_boostMassCost))
            {
                CreatePointBehindPlayer();
            }
        }

        private void CreatePointBehindPlayer()
        {
            if (_pointSpawnTransform != null)
            {
                _pointSpawnService.SpawnAt(_pointSpawnTransform);
            }
        }

        protected override void DeathCheck(float capacity)
        {
            if (capacity <= MinimumCapacity)
            {
                Defeat();
            }
        }

        private void SetBordersTrigger(BordersTrigger bordersTrigger)
        {
            if (_bordersTrigger == bordersTrigger)
            {
                return;
            }

            UnsubscribeFromBorderEvent();
            _bordersTrigger = bordersTrigger;
            SubscribeToBorderEvent();
        }

        private void SubscribeToBorderEvent()
        {
            if (_bordersTrigger == null || _borderEventSubscribed)
            {
                return;
            }

            _bordersTrigger.PlayerTriggered += OnBorderHit;
            _borderEventSubscribed = true;
        }

        private void UnsubscribeFromBorderEvent()
        {
            if (_bordersTrigger == null || !_borderEventSubscribed)
            {
                return;
            }

            _bordersTrigger.PlayerTriggered -= OnBorderHit;
            _borderEventSubscribed = false;
        }

        private void OnBorderHit(Player triggeringPlayer)
        {
            if (IsServer && triggeringPlayer == this)
            {
                Shrink(
                    Mathf.Max(
                        _boostMassCost,
                        Capacity * _borderMassLoss));
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer ||
                !_servicesReady ||
                !_gameStateService.IsGameplayActive ||
                other == null ||
                _isDefeated)
            {
                return;
            }

            if (other.TryGetComponent(out Point point))
            {
                Grow(point.Capacity * _foodGrowthMultiplier);
                _pointRespawnService.Respawn(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                Shrink(comet.Capacity * _cometDamageMultiplier);
                _cometRespawnService.Respawn(comet);
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                if (Capacity >= enemy.Capacity * _eatEnemySizeRatio)
                {
                    _enemyRespawnService.Respawn(enemy);
                }
                else
                {
                    Defeat();
                }
            }
        }

        private void Defeat()
        {
            if (_isDefeated)
            {
                return;
            }

            _isDefeated = true;

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            TriggerDefeatRpc();
        }

        [Rpc(SendTo.Owner)]
        private void TriggerDefeatRpc()
        {
            _gameStateService?.FinishGame();
            _ = ShutdownSessionAsync();
        }

        private async Awaitable ShutdownSessionAsync()
        {
            if (_networkSessionService == null)
            {
                return;
            }

            try
            {
                await _networkSessionService.ShutdownAndReturnToMenuAsync();
            }
            catch (OperationCanceledException)
            {
                // The player or application is shutting down.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }
    }
}
