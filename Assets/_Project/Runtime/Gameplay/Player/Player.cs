using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public sealed partial class Player : PlanetScale
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
        [SerializeField, Min(0f)] private float _continueProtectionTime = 5f;
        [SerializeField, Min(0.01f)] private float _initialCapacity = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _continueMassFraction = 0.5f;

        [Header("Food magnet")]
        [SerializeField, Min(0f)] private float _foodAttractionRadius = 0.3f;
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

        private readonly NetworkVariable<Color32> _networkColor = new(
            new Color32(255, 255, 255, 255),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<Food> _foodSpawnService;
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
        private readonly Collider2D[] _foodBuffer = new Collider2D[FoodAttractionBuffer];
        private ContactFilter2D _foodFilter;

        public bool IsDefeated => IsSpawned && _networkDefeated.Value;
        public bool IsSpawnProtected => _invincibilityTimeRemaining > 0f;
        public bool CanBoost => !IsDefeated && Capacity > MinCapacity + _boostMassCost;

        private bool CanActInGameplay => !IsDefeated && _gameStateService.IsGameplayActive;

        public event Action Defeated;
        public event Action Revived;
        public static event Action<DefeatAnnouncement> DefeatAnnounced;
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
            BordersTrigger bordersTrigger,
            PlanetSkinCatalog skinCatalog,
            IPlayerRegistry playerRegistry)
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
            _foodFilter = ContactFilter2D.noFilter;
            _foodFilter.SetLayerMask(LayerMask.GetMask(Constants.FoodLayerName));
            Capacity = _initialCapacity;
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

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void SetBoostRpc(bool boosting)
        {
            SetServerBoosting(CanActInGameplay && boosting);
        }

        private void SetServerBoosting(bool boosting)
        {
            _serverBoosting = boosting;
            _networkBoosting.Value = boosting;
        }

        private void ResetBoost()
        {
            SetServerBoosting(false);
            _boostTimer = 0f;
        }

        private void UpdateBoost(float deltaTime)
        {
            if (!_serverBoosting || !CanActInGameplay)
            {
                _boostTimer = 0f;
                if (_serverBoosting)
                {
                    SetServerBoosting(false);
                }

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
            if (_pointSpawnTransform == null)
            {
                return;
            }

            Vector2 spawnPosition = _pointSpawnTransform.position;
            if (_rigidbody2D != null)
            {
                Vector2 velocity = _rigidbody2D.linearVelocity;
                if (velocity.sqrMagnitude > 0.001f)
                {
                    spawnPosition = (Vector2)transform.position - velocity.normalized * Capacity * 0.3f;
                    spawnPosition = Constants.ClampToWorld(spawnPosition);
                    _pointSpawnTransform.position = spawnPosition;
                }
            }

            _foodSpawnService.SpawnAt(_pointSpawnTransform, _boostMassCost * BoostDropNutritionFraction);
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
                !IsSpawnProtected)
            {
                Shrink(Mathf.Max(_boostMassCost, Capacity * _borderMassLoss));
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SceneContainers.Attach(transform, SceneContainers.Players);
            _networkDefeated.OnValueChanged += OnDefeatedChanged;
            _networkBoosting.OnValueChanged += OnBoostingChanged;
            _networkSpawnProtected.OnValueChanged += OnSpawnProtectionChanged;
            _networkColor.OnValueChanged += OnColorChanged;
            _networkSkin.OnValueChanged += OnSkinChanged;

            if (IsServer)
            {
                _networkDefeated.Value = false;
                Capacity = _initialCapacity;
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

            _visualEffects = GetComponent<PlayerVisualEffects>();
            _visualEffects?.SetLocalAudio(IsOwner);
            ApplyVisualState();

            if (IsOwner)
            {
                SubmitNicknameRpc(_playerProfileService.Nickname);
                SetColorRpc(_playerProfileService.PreferredColor);
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
            _networkColor.OnValueChanged -= OnColorChanged;
            _networkSkin.OnValueChanged -= OnSkinChanged;
            base.OnNetworkDespawn();
        }

        private void OnSkinChanged(byte _, byte skin)
        {
            ApplySkin(skin);
        }

        private void ApplySkin(byte skin)
        {
            if (!_skinCatalog.IsValid(skin) || !TryGetComponent(out SpriteRenderer body))
            {
                return;
            }

            Sprite sprite = _skinCatalog.Get(skin).Sprite;
            if (sprite != null)
            {
                body.sprite = sprite;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetSkinRpc(byte skin)
        {
            if (_skinCatalog.IsValid(skin))
            {
                _networkSkin.Value = skin;
            }
        }

        private void OnColorChanged(Color32 _, Color32 color)
        {
            _visualEffects?.SetBaseColor(color);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SetColorRpc(Color32 color)
        {
            _networkColor.Value = color;
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

            _visualEffects.SetBaseColor(_networkColor.Value);
            ApplySkin(_networkSkin.Value);
            _visualEffects.SetBoosting(_networkBoosting.Value);
            _visualEffects.SetSpawnProtected(_networkSpawnProtected.Value);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void SubmitNicknameRpc(FixedString64Bytes nickname)
        {
            SetDisplayName(NicknameRules.Normalize(nickname.ToString()));
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            UpdateBoost(Time.deltaTime);

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

        private void AttractFood()
        {
            int hitCount = Physics2D.OverlapCircle(
                transform.position, Capacity * 0.5f + _foodAttractionRadius, _foodFilter, _foodBuffer);
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
                !CanActInGameplay ||
                other == null)
            {
                return;
            }

            bool protectedFromHarm = IsSpawnProtected;

            if (other.TryGetComponent(out Player otherPlayer))
            {
                if (!protectedFromHarm)
                {
                    TryEatPlayer(otherPlayer);
                }
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                if (Capacity >= enemy.Capacity * _eatSizeRatio)
                {
                    AbsorbVictim(enemy.Capacity);
                    NotifyKillRpc(enemy.DisplayName, Constants.CapacityToScore(enemy.Capacity));
                    _enemyRespawnService.Respawn(enemy);
                }
            }
            else if (!protectedFromHarm || other.TryGetComponent<Food>(out _))
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
            AbsorbVictim(otherPlayer.Capacity);
            otherPlayer.Defeat(DisplayName, OwnerClientId);
        }

        [Rpc(SendTo.Everyone)]
        private void AnnounceDefeatRpc(FixedString64Bytes killerName, ulong killerClientId)
        {
            DefeatAnnounced?.Invoke(new DefeatAnnouncement(
                killerName.ToString(), killerClientId, DisplayName, OwnerClientId));
        }

        [Rpc(SendTo.Owner)]
        private void NotifyKillRpc(FixedString64Bytes victimName, int score)
        {
            Killed?.Invoke(victimName.ToString(), score);
        }

        public void Defeat(string killerName = null, ulong killerClientId = Constants.UnassignedClientId)
        {
            if (!IsServer || IsDefeated)
            {
                return;
            }

            if (!string.IsNullOrEmpty(killerName))
            {
                AnnounceDefeatRpc(killerName, killerClientId);
            }

            ResetBoost();
            _capacityAtDefeat = Capacity;
            _lootDrop = _lootSpawnService?.SpawnLoot(transform.position, Capacity);

            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            _networkDefeated.Value = true;
        }

        private void OnDefeatedChanged(bool _, bool isDefeated)
        {
            SetBodyActive(!isDefeated);

            if (isDefeated)
            {
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.linearVelocity = Vector2.zero;
                }

                GameVfx.Death(transform.position, _networkColor.Value, Capacity);

                if (IsOwner)
                {
                    Defeated?.Invoke();
                }

                return;
            }

            if (IsOwner)
            {
                TeleportTo(Constants.RandomWorldPosition());
                Revived?.Invoke();
            }
        }

        private void TeleportTo(Vector2 position)
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _rigidbody2D.position = position;
            }

            if (TryGetComponent(out NetworkTransform networkTransform))
            {
                networkTransform.Teleport(position, transform.rotation, transform.localScale);
            }
            else
            {
                transform.position = position;
            }
        }

        private void SetBodyActive(bool active)
        {
            if (TryGetComponent(out Collider2D bodyCollider))
            {
                bodyCollider.enabled = active;
            }

            if (TryGetComponent(out SpriteRenderer bodyRenderer))
            {
                bodyRenderer.enabled = active;
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void ContinueRpc()
        {
            if (!IsServer ||
                !IsDefeated ||
                _continueUsedThisLife)
            {
                return;
            }

            _continueUsedThisLife = true;
            _lootDrop?.Discard();
            _lootDrop = null;
            ResetBoost();
            _invincibilityTimeRemaining = _continueProtectionTime;
            Capacity = Mathf.Max(_initialCapacity, _capacityAtDefeat * _continueMassFraction);
            _networkDefeated.Value = false;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        public void RespawnRpc()
        {
            if (!IsServer ||
                !IsDefeated)
            {
                return;
            }

            _continueUsedThisLife = false;
            _lootDrop = null;
            ResetBoost();
            _invincibilityTimeRemaining = _spawnInvincibilityTime;
            Capacity = _initialCapacity;
            _networkDefeated.Value = false;
        }
    }
}
