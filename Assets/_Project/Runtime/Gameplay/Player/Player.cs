using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
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

        [Header("Spawn protection")]
        [FormerlySerializedAs("_respawnInvincibilityTime")]
        [SerializeField, Min(0f)] private float _spawnInvincibilityTime = 2f;
		[SerializeField, Min(0.01f)] private float _initialCapacity = 0.1f;

        private readonly NetworkVariable<bool> _networkDefeated = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

		private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<Point> _pointSpawnService;
        private IGameStateService _gameStateService;

        private bool _servicesReady;
        private bool _borderEventSubscribed;
        private float _invincibilityTimeRemaining;

        public bool IsDefeated => IsSpawned && _networkDefeated.Value;
        public bool CanBoost => !IsDefeated && Capacity > MinCapacity + _boostMassCost;

        public event Action Defeated;

        protected override float FoodGrowthMultiplier => _playerFoodGrowthMultiplier;
        protected override float CometDamageMultiplier => _playerCometDamageMultiplier;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Point> pointRespawnService,
            IRespawnService<Enemy> enemyRespawnService,
            ISpawnService<Point> pointSpawnService,
            IGameStateService gameStateService,
            BordersTrigger bordersTrigger)
        {
            CometRespawnService = cometRespawnService ?? throw new ArgumentNullException(nameof(cometRespawnService));
            PointRespawnService = pointRespawnService ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _enemyRespawnService = enemyRespawnService ?? throw new ArgumentNullException(nameof(enemyRespawnService));
            _pointSpawnService = pointSpawnService ?? throw new ArgumentNullException(nameof(pointSpawnService));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
            SetBordersTrigger(bordersTrigger ?? throw new ArgumentNullException(nameof(bordersTrigger)));
            _servicesReady = true;
        }

        protected override void Awake()
        {
            base.Awake();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            Capacity = _initialCapacity;
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
            if (!IsOwner ||
                IsDefeated ||
                _gameStateService?.IsGameplayActive != true)
            {
                return;
            }

            ApplyBoostRpc();
        }

        [Rpc(SendTo.Server)]
        private void ApplyBoostRpc()
        {
            if (!_servicesReady ||
                IsDefeated ||
                !_gameStateService.IsGameplayActive ||
                Capacity <= MinCapacity + _boostMassCost)
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
            if (IsServer && capacity <= MinCapacity)
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
            if (IsServer &&
                !IsDefeated &&
                triggeringPlayer == this &&
                _invincibilityTimeRemaining <= 0f)
            {
                Shrink(Mathf.Max(_boostMassCost, Capacity * _borderMassLoss));
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _networkDefeated.OnValueChanged += OnDefeatedChanged;

            if (IsServer)
            {
                _networkDefeated.Value = false;
                Capacity = _initialCapacity;
                _invincibilityTimeRemaining = _spawnInvincibilityTime;
            }

            if (_networkDefeated.Value)
            {
                OnDefeatedChanged(false, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkDefeated.OnValueChanged -= OnDefeatedChanged;
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsServer &&
                !IsDefeated &&
                _invincibilityTimeRemaining > 0f)
            {
                _invincibilityTimeRemaining -= Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer ||
                !_servicesReady ||
                !_gameStateService.IsGameplayActive ||
                IsDefeated ||
                other == null ||
                _invincibilityTimeRemaining > 0f)
            {
                return;
            }

            if (other.TryGetComponent(out Enemy enemy))
            {
                if (Capacity >= enemy.Capacity * _eatEnemySizeRatio)
                {
                    Grow(enemy.Capacity);
                    _enemyRespawnService.Respawn(enemy);
                }
            }
            else
            {
                HandleEntityCollision(other);
            }
        }

        public void Defeat()
        {
            if (!IsServer || IsDefeated)
            {
                return;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            _networkDefeated.Value = true;
        }

        private void OnDefeatedChanged(bool _, bool isDefeated)
        {
            if (!isDefeated)
            {
                return;
            }

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            if (IsOwner)
            {
                Defeated?.Invoke();
            }
        }

    }
}
