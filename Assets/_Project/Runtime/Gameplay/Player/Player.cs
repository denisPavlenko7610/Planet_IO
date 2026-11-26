using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
	public sealed partial class Player : PlanetBody
    {
        public readonly struct DefeatAnnouncement
        {
            public readonly string KillerName;
            public readonly ulong KillerClientId;
            public readonly string VictimName;
            public readonly ulong VictimClientId;

            public DefeatAnnouncement(string killerName, ulong killerClientId, string victimName, ulong victimClientId)
            {
                KillerName = killerName;
                KillerClientId = killerClientId;
                VictimName = victimName;
                VictimClientId = victimClientId;
            }
        }

        private BordersTrigger _bordersTrigger;
        private Rigidbody2D _rigidbody2D;

        [Header("Mass balance")]
        [SerializeField, Min(0.001f)] private float _playerFoodGrowthMultiplier = 0.015f;
        [SerializeField, Min(0.001f)] private float _playerCometDamageMultiplier = CometRules.DefaultDamageFraction;
        [SerializeField, Min(0.001f)] private float _boostMassCost = 0.004f;
        [SerializeField, Min(0.05f)] private float _boostMassConsumptionInterval = 0.15f;
        [SerializeField, Range(0f, 0.5f)] private float _borderMassLoss = 0.08f;

        [Header("Boost food")]
        [SerializeField] private Transform _pointSpawnTransform;

        [Header("Spawn protection")]
        [FormerlySerializedAs("_respawnInvincibilityTime")]
        [SerializeField, Min(0f)] private float _spawnInvincibilityTime = 2f;
        [SerializeField, Min(0f)] private float _continueProtectionTime = 5f;
		[SerializeField, Range(0f, 1f)] private float _continueMassFraction = 1f;

        [Header("Food magnet")]
        [SerializeField, Min(0f)] private float _foodAttractionRadius = 0.2f;
        [SerializeField, Min(0f)] private float _foodAttractionSpeed = 5f;

        private const int FoodAttractionBuffer = 64;
        public const float BoostDropNutritionFraction = 0.8f;

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

        private readonly NetworkVariable<byte> _networkSkin = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<FoodRecord> _foodSpawnService;
        private IGameStateService _gameStateService;
        private IPlayerProfileService _playerProfileService;
        private ILootSpawnService _lootSpawnService;
        private ILootDrop _lootDrop;
        private IPlayerRegistry _playerRegistry;
        private bool _borderEventSubscribed;
        private bool _serverBoosting;
        private bool _continueUsedThisLife;
        private float _boostTimer;
        private float _invincibilityTimeRemaining;
        private float _capacityAtDefeat;
        private PlayerVisualEffects _visualEffects;
        private PlanetSkinCatalog _skinCatalog;
		private readonly System.Collections.Generic.List<FoodRecord> _foodBuffer = new(FoodAttractionBuffer);

        public bool IsDefeated => IsSpawned && _networkDefeated.Value;
        public bool IsSpawnProtected => _invincibilityTimeRemaining > 0f;
        public bool CanBoost => !IsDefeated && Capacity > Mathf.Max(InitialCapacity, MinCapacity) + BoostCost;
        public bool IsBoosting => IsSpawned && _networkBoosting.Value;

        private bool CanActInGameplay => !IsDefeated && _gameStateService.IsGameplayActive;

        public event Action Defeated;
        public event Action Revived;
        public static event Action<DefeatAnnouncement> DefeatAnnounced;
        public event Action<string, int> Killed;

        protected override float IncomingDamageMultiplier =>
            BotDifficultyRules.PlayerIncomingDamage(DifficultyLevel, DifficultySessionMode);

        protected override float FoodGrowthMultiplier =>
            _playerFoodGrowthMultiplier * PerkRules.FoodMultiplier(PerkMask);
        protected override float CometDamageMultiplier => _playerCometDamageMultiplier;
        protected override string GetFallbackDisplayName() =>
            IsSpawned ? $"Player {OwnerClientId}" : NicknameRules.DefaultNickname;

        [Inject]
        public void Construct(
			IRespawnService<Comet> cometRespawnService,
			IFoodService pointRespawnService,
			IRespawnService<Enemy> enemyRespawnService,
			ISpawnService<FoodRecord> pointSpawnService,
			IGameStateService gameStateService,
			IPlayerProfileService playerProfileService,
			ILootSpawnService lootSpawnService,
			BordersTrigger bordersTrigger,
			PlanetSkinCatalog skinCatalog,
			IPlayerRegistry playerRegistry
		)
        {
            _skinCatalog = skinCatalog;
            CometRespawnService = cometRespawnService;
            FoodRespawnService = pointRespawnService;
            _enemyRespawnService = enemyRespawnService;
            _foodSpawnService = pointSpawnService;
            _gameStateService = gameStateService;
            _playerProfileService = playerProfileService;
            _lootSpawnService = lootSpawnService;
            SetBordersTrigger(bordersTrigger);
            _playerRegistry = playerRegistry;
        }

        protected override void Awake()
        {
            base.Awake();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            InitializeAbilities();
        }

        private void OnEnable()
        {
            _playerRegistry.Register(this);
            SubscribeToBorderEvent();
        }

        private void OnDisable()
        {
            _playerRegistry.Unregister(this);
            UnsubscribeFromBorderEvent();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SceneContainers.Attach(transform, SceneContainers.Players);
            _networkDefeated.OnValueChanged += OnDefeatedChanged;
            _networkBoosting.OnValueChanged += OnBoostingChanged;
            _networkSpawnProtected.OnValueChanged += OnSpawnProtectionChanged;
            _networkSkin.OnValueChanged += OnSkinChanged;
            SubscribePerks();
            AttachAbilityViews();

            if (IsServer && _pendingMigration == null)
            {
                _highestTier = TierRules.MinTier;
                _networkDefeated.Value = false;
                Capacity = InitialCapacity;
                _invincibilityTimeRemaining = _spawnInvincibilityTime;
                SetServerBoosting(false);
                _networkSpawnProtected.Value = true;
                _continueUsedThisLife = false;
                _boostTimer = 0f;
            }

            if (IsOwner)
            {
                _playerRegistry.SetLocal(this);
            }

            ApplyPendingMigration();
            _visualEffects = GetComponent<PlayerVisualEffects>();
            _visualEffects?.SetLocalAudio(IsOwner);
            ApplyVisualState();
            SetBodyActive(!IsDefeated);

            if (IsOwner && !WasMigrationRestored)
            {
                SubmitNicknameRpc(_playerProfileService.Nickname);
                SetSkinRpc((byte)_playerProfileService.SelectedSkin);
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
            _networkSkin.OnValueChanged -= OnSkinChanged;
            UnsubscribePerks();
            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (!IsServer || _migration?.IsRecovering == true)
            {
                return;
            }

            ApplyPendingCapacityRestore();
            UpdateBoost(Time.deltaTime);
            UpdatePerkOffers(Time.deltaTime);
            UpdateAbilities(Time.deltaTime);

            if (!IsDefeated && IsSpawnProtected)
            {
                _invincibilityTimeRemaining -= Time.deltaTime;
            }

            bool spawnProtected = !IsDefeated && IsSpawnProtected;
            if (_networkSpawnProtected.Value != spawnProtected)
            {
                _networkSpawnProtected.Value = spawnProtected;
            }

            if (CanActInGameplay && _foodAttractionRadius > 0f)
            {
                AttractFood();
            }
        }
    }
}