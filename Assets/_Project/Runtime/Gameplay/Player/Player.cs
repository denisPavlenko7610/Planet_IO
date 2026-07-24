using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed class Player : PlanetScale
    {
        private BordersTrigger _bordersTrigger;
        private Rigidbody2D _rigidbody2D;

        [Header("Mass balance")]
        [SerializeField, Min(0.001f)] private float _playerFoodGrowthMultiplier = 0.015f;
        [SerializeField, Min(0.001f)] private float _playerCometDamageMultiplier = 0.02f;
        [SerializeField, Min(0.001f)] private float _boostMassCost = 0.004f;
        [SerializeField, Range(0f, 0.5f)] private float _borderMassLoss = 0.08f;
        [SerializeField, Min(1f)] private float _eatEnemySizeRatio = 1.08f;

        [Header("Boost food")]
        [SerializeField] private Transform _pointSpawnTransform;

        private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<Point> _pointSpawnService;
        private INetworkSessionService _networkSessionService;
        private IGameStateService _gameStateService;
        private bool _servicesReady;
        private bool _borderEventSubscribed;
        private bool _isDefeated;
        private float _invincibilityTimeRemaining;

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
            CometRespawnService = cometRespawnService ?? throw new ArgumentNullException(nameof(cometRespawnService));
            PointRespawnService = pointRespawnService ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _enemyRespawnService = enemyRespawnService ?? throw new ArgumentNullException(nameof(enemyRespawnService));
            _pointSpawnService = pointSpawnService ?? throw new ArgumentNullException(nameof(pointSpawnService));
            _networkSessionService = networkSessionService ?? throw new ArgumentNullException(nameof(networkSessionService));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
            SetBordersTrigger(bordersTrigger ?? throw new ArgumentNullException(nameof(bordersTrigger)));
            FoodGrowthMultiplier = _playerFoodGrowthMultiplier;
            CometDamageMultiplier = _playerCometDamageMultiplier;
            _servicesReady = true;
        }

        protected override void Awake()
        {
            base.Awake();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
            SubscribeToBorderEvent();
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
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
            if (IsServer && capacity <= MinimumCapacity)
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
            if (IsServer && triggeringPlayer == this && _invincibilityTimeRemaining <= 0f)
            {
                Shrink(Mathf.Max(_boostMassCost, Capacity * _borderMassLoss));
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
            {
                _invincibilityTimeRemaining = 2f;
            }
        }

        private void Update()
        {
            if (IsServer && _invincibilityTimeRemaining > 0f)
            {
                _invincibilityTimeRemaining -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer ||
                !_servicesReady ||
                !_gameStateService.IsGameplayActive ||
                other == null ||
                _isDefeated ||
                _invincibilityTimeRemaining > 0f)
            {
                return;
            }

            if (other.TryGetComponent(out Enemy enemy))
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
            else
            {
                HandleEntityCollision(other);
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
				LoggerIO.LogError("The player or application is shutting down");
            }
            catch (Exception exception)
            {
                LoggerIO.LogException(exception, this);
            }
        }
    }
}
