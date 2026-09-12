using System;
using Unity.Collections;
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
        [SerializeField, Min(0.05f)] private float _boostMassConsumptionInterval = 0.15f;
        [SerializeField, Range(0f, 0.5f)] private float _borderMassLoss = 0.08f;
        [SerializeField, Min(1f)] private float _eatSizeRatio = 1.08f;

        [Header("Boost food")]
        [SerializeField] private Transform _pointSpawnTransform;

        [Header("Spawn protection")]
        [FormerlySerializedAs("_respawnInvincibilityTime")]
        [SerializeField, Min(0f)] private float _spawnInvincibilityTime = 2f;
		[SerializeField, Min(0.01f)] private float _initialCapacity = 0.1f;

        [Header("Food magnet")]
        [SerializeField, Min(0f)] private float _foodAttractionRadius = 4f;
        [SerializeField, Min(0f)] private float _foodAttractionSpeed = 5f;

        private const int FoodAttractionBuffer = 16;

        private readonly NetworkVariable<bool> _networkDefeated = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _networkBoosting = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<bool> _networkSpawnProtected = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

		private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<Food> _foodSpawnService;
        private IGameStateService _gameStateService;
        private IPlayerProfileService _playerProfileService;
        private ILootSpawnService _lootSpawnService;

        private bool _servicesReady;
        private bool _borderEventSubscribed;
        private bool _serverBoosting;
        private float _boostTimer;
        private float _invincibilityTimeRemaining;
        private PlayerVisualEffects _visualEffects;
        private readonly Collider2D[] _foodBuffer = new Collider2D[FoodAttractionBuffer];

        public bool IsDefeated => IsSpawned && _networkDefeated.Value;
        public bool IsSpawnProtected => _invincibilityTimeRemaining > 0f;
        public bool CanBoost => !IsDefeated && Capacity > MinCapacity + _boostMassCost;

        public event Action Defeated;
        public event Action<string, int> Killed;

        protected override float FoodGrowthMultiplier => _playerFoodGrowthMultiplier;
        protected override float CometDamageMultiplier => _playerCometDamageMultiplier;
        protected override string GetFallbackDisplayName() =>
            IsSpawned ? $"Player {OwnerClientId}" : NicknameRules.DefaultNickname;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Food> pointRespawnService,
            IRespawnService<Enemy> enemyRespawnService,
            ISpawnService<Food> pointSpawnService,
            IGameStateService gameStateService,
            IPlayerProfileService playerProfileService,
            ILootSpawnService lootSpawnService,
            BordersTrigger bordersTrigger)
        {
            CometRespawnService = cometRespawnService ?? throw new ArgumentNullException(nameof(cometRespawnService));
            FoodRespawnService = pointRespawnService ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _enemyRespawnService = enemyRespawnService ?? throw new ArgumentNullException(nameof(enemyRespawnService));
            _foodSpawnService = pointSpawnService ?? throw new ArgumentNullException(nameof(pointSpawnService));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
            _playerProfileService = playerProfileService ?? throw new ArgumentNullException(nameof(playerProfileService));
            _lootSpawnService = lootSpawnService ?? throw new ArgumentNullException(nameof(lootSpawnService));
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

        [Rpc(SendTo.Server)]
        public void SetBoostRpc(bool boosting)
        {
            if (!_servicesReady ||
                IsDefeated ||
                !_gameStateService.IsGameplayActive)
            {
                SetServerBoosting(false);
                return;
            }

            SetServerBoosting(boosting);
        }

        private void SetServerBoosting(bool boosting)
        {
            _serverBoosting = boosting;
            _networkBoosting.Value = boosting;
        }

        private void UpdateBoost(float deltaTime)
        {
            if (!_serverBoosting ||
                IsDefeated ||
                !_servicesReady ||
                !_gameStateService.IsGameplayActive)
            {
                _boostTimer = 0f;
                return;
            }

            _boostTimer -= deltaTime;
            if (_boostTimer > 0f)
            {
                return;
            }

            _boostTimer = _boostMassConsumptionInterval;

            if (!CanBoost)
            {
                SetServerBoosting(false);
                return;
            }

            if (Shrink(_boostMassCost))
            {
                CreateFoodBehindPlayer();
            }
        }

        private void CreateFoodBehindPlayer()
        {
            if (_pointSpawnTransform != null)
            {
                _foodSpawnService.SpawnAt(_pointSpawnTransform);
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
            _networkBoosting.OnValueChanged += OnBoostingChanged;
            _networkSpawnProtected.OnValueChanged += OnSpawnProtectionChanged;

            if (IsServer)
            {
                _networkDefeated.Value = false;
                Capacity = _initialCapacity;
                _invincibilityTimeRemaining = _spawnInvincibilityTime;
                SetServerBoosting(false);
                _networkSpawnProtected.Value = true;
                _boostTimer = 0f;
            }

            _visualEffects = GetComponent<PlayerVisualEffects>();
            ApplyVisualState();

            if (IsOwner && _playerProfileService != null)
            {
                SubmitNicknameRpc(_playerProfileService.Nickname);
            }

            if (_networkDefeated.Value)
            {
                OnDefeatedChanged(false, true);
            }
        }

        public override void OnNetworkDespawn()
        {
            _networkDefeated.OnValueChanged -= OnDefeatedChanged;
            _networkBoosting.OnValueChanged -= OnBoostingChanged;
            _networkSpawnProtected.OnValueChanged -= OnSpawnProtectionChanged;
            base.OnNetworkDespawn();
        }

        private void OnBoostingChanged(bool _, bool boosting)
        {
            _visualEffects?.SetBoosting(boosting);
        }

        private void OnSpawnProtectionChanged(bool _, bool protection)
        {
            _visualEffects?.SetSpawnProtected(protection);
        }

        private void ApplyVisualState()
        {
            if (_visualEffects == null)
            {
                return;
            }

            _visualEffects.SetBoosting(_networkBoosting.Value);
            _visualEffects.SetSpawnProtected(_networkSpawnProtected.Value);
        }

        [Rpc(SendTo.Server)]
        private void SubmitNicknameRpc(FixedString64Bytes nickname)
        {
            SetDisplayName(nickname.ToString());
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            UpdateBoost(Time.deltaTime);

            if (!IsDefeated && _invincibilityTimeRemaining > 0f)
            {
                _invincibilityTimeRemaining -= Time.deltaTime;
            }

            bool spawnProtected = !IsDefeated && _invincibilityTimeRemaining > 0f;
            if (_networkSpawnProtected.Value != spawnProtected)
            {
                _networkSpawnProtected.Value = spawnProtected;
            }

            if (_servicesReady &&
                !IsDefeated &&
                _gameStateService.IsGameplayActive &&
                _foodAttractionRadius > 0f)
            {
                AttractFood();
            }
        }

        private void AttractFood()
        {
            int hitCount = Physics2D.OverlapCircle(
                transform.position, _foodAttractionRadius, ContactFilter2D.noFilter, _foodBuffer);
            float pullStep = _foodAttractionSpeed * Time.deltaTime;

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D hit = _foodBuffer[index];
                if (hit == null || !hit.TryGetComponent(out Food food))
                {
                    continue;
                }

                Transform foodTransform = food.transform;
                Vector2 offset = (Vector2)transform.position - (Vector2)foodTransform.position;
                float distance = offset.magnitude;
                if (distance < 0.01f)
                {
                    continue;
                }

                foodTransform.position += (Vector3)(offset / distance * Mathf.Min(pullStep, distance));
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

            if (other.TryGetComponent(out Player otherPlayer))
            {
                TryEatPlayer(otherPlayer);
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                if (Capacity >= enemy.Capacity * _eatSizeRatio)
                {
                    Grow(enemy.Capacity);
                    NotifyKillRpc(enemy.DisplayName, Constants.CapacityToScore(enemy.Capacity));
                    _enemyRespawnService.Respawn(enemy);
                }
            }
            else
            {
                HandleEntityCollision(other);
            }
        }

        private void TryEatPlayer(Player otherPlayer)
        {
            if (otherPlayer == this ||
                otherPlayer.IsDefeated ||
                otherPlayer.IsSpawnProtected ||
                Capacity < otherPlayer.Capacity * _eatSizeRatio)
            {
                return;
            }

            int score = Constants.CapacityToScore(otherPlayer.Capacity);
            NotifyKillRpc(otherPlayer.DisplayName, score);
            Grow(otherPlayer.Capacity);
            otherPlayer.Defeat();
        }

        [Rpc(SendTo.Owner)]
        private void NotifyKillRpc(FixedString64Bytes victimName, int score)
        {
            Killed?.Invoke(victimName.ToString(), score);
        }

        public void Defeat()
        {
            if (!IsServer || IsDefeated)
            {
                return;
            }

            _lootSpawnService?.SpawnLoot(transform.position, Capacity);

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

            SetDefeatedVisuals();

            if (IsOwner)
            {
                Defeated?.Invoke();
            }
        }

        private void SetDefeatedVisuals()
        {
            if (TryGetComponent(out Collider2D bodyCollider))
            {
                bodyCollider.enabled = false;
            }

            foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.enabled = false;
            }
        }

    }
}
