using System;
using RD.Attributes;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D), typeof(EnemyMovement))]
	public sealed partial class Enemy : PlanetBody
    {
        [Header("Presentation")]
        [SerializeField] private EnemyConfig _enemyConfig;
        [SerializeField, Assign] private SpriteRenderer _spriteRenderer;

        [Header("Balance")]
        [SerializeField, Min(0f)] private float _enemyFoodGrowthMultiplier = 0.01f;
        [SerializeField, Min(0f)] private float _enemyCometDamageMultiplier = CometRules.DefaultDamageFraction;

        private IGameStateService _gameStateService;
        private IRespawnService<Enemy> _enemyRespawnService;
        private bool _servicesReady;
		private float _magnetTimer;

        private readonly NetworkVariable<bool> _networkBoosting = new(false);
        public bool IsBoosting => IsSpawned && _networkBoosting.Value;

        internal void SetBoosting(bool value)
        {
            if (IsServer && IsSpawned)
            {
                _networkBoosting.Value = value;
            }
        }

        internal bool TrySpendBoostMass(out float spent)
        {
			spent = BotDifficultyRules.BoostCostFor(Capacity);

            return IsServer &&
                IsSpawned &&
                BotDifficultyRules.CanAffordBoost(Capacity, Mathf.Max(InitialCapacity, MinCapacity)) &&
                Shrink(spent);
        }

        protected override bool IsCombatActive => _servicesReady && _gameStateService.IsGameplayActive;

        protected override float IncomingDamageMultiplier => BotDifficultyRules.BotIncomingDamage(DifficultyLevel);
        protected override float MoonDamageMultiplier => BotDifficultyRules.BotOutgoingDamage(DifficultyLevel);

        protected override float FoodGrowthMultiplier =>
			_enemyFoodGrowthMultiplier * BotDifficultyRules.FoodGrowthScale(DifficultyLevel);
        protected override float CometDamageMultiplier => _enemyCometDamageMultiplier;
        protected override string GetFallbackDisplayName() => BotNames.Get(NetworkObjectId);

        [Inject]
        public void Construct(
			IRespawnService<Comet> cometRespawnService,
			IFoodService pointRespawnService,
			IRespawnService<Enemy> enemyRespawnService,
			IGameStateService gameStateService
		)
        {
            CometRespawnService = cometRespawnService ?? throw new ArgumentNullException(nameof(cometRespawnService));
            FoodRespawnService = pointRespawnService ?? throw new ArgumentNullException(nameof(pointRespawnService));
            _enemyRespawnService = enemyRespawnService ?? throw new ArgumentNullException(nameof(enemyRespawnService));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
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
            SceneContainers.Attach(transform, SceneContainers.Enemies);

            if (IsServer)
            {
                _appearanceSeed.Value = NetworkObjectId;
                SetDisplayName(BotNames.Get(NetworkObjectId));
                _networkBoosting.Value = false;
            }

            SetDeterministicSprite();
            ApplyDeterministicTint();
            _appearanceSeed.OnValueChanged += AppearanceChanged;
            _networkBoosting.OnValueChanged += OnBoostChanged;
            OnBoostChanged(false, _networkBoosting.Value);
        }

        public override void OnNetworkDespawn()
        {
            _appearanceSeed.OnValueChanged -= AppearanceChanged;
            _networkBoosting.OnValueChanged -= OnBoostChanged;
            base.OnNetworkDespawn();
        }

        private void OnBoostChanged(bool _, bool boosting)
        {
            if (_spriteRenderer == null)
            {
                return;
            }

            Color color = PlayerPalette.GetColor((int)AppearanceSeed);
            _spriteRenderer.color = boosting ? Color.Lerp(color, new Color(1f, 0.85f, 0.4f), 0.65f) : color;
        }

        public void PlayDeathEffect()
        {
            if (IsServer && IsSpawned)
            {
                PlayDeathEffectRpc(transform.position, Capacity);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PlayDeathEffectRpc(Vector2 position, float size)
        {
            Color color = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
            GameVfx.Death(position, color, size);
        }

        private void ApplyDeterministicTint()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = PlayerPalette.GetColor((int)AppearanceSeed);
            }
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
				TryEatEnemy(otherEnemy);
            }
            else if (collider2D.TryGetComponent(out Player player))
            {
				TryEatPlayer(player);
            }
            else
            {
                HandleEntityCollision(collider2D);
            }
        }

		protected override void ResolvePlanetContact(PlanetBody other)
		{
			if (other is Enemy otherEnemy)
			{
				TryEatEnemy(otherEnemy);
			}
			else if (other is Player player)
			{
				TryEatPlayer(player);
			}
		}

		protected override void ServerFeedingStep(float deltaTime)
		{
			if (!FoodSweepRules.Advance(ref _magnetTimer, deltaTime))
			{
				return;
			}

			if (!BotDifficultyRules.UsesFoodMagnet(DifficultyLevel))
			{
				return;
			}

			PullFoodTowardsBody(FoodMagnetRules.BaseReach, 1f, FoodMagnetRules.PullSpeed, FoodSweepRules.Interval);
		}

		private void TryEatEnemy(Enemy otherEnemy)
		{
			if (otherEnemy == this || !CanDevour(otherEnemy))
			{
				return;
			}

			AbsorbVictim(otherEnemy);
			_enemyRespawnService.Respawn(otherEnemy);
		}

		private void TryEatPlayer(Player player)
		{
			if (player.IsDefeated ||
				player.IsSpawnProtected ||
				!CanDevour(player) ||
				player.TryConsumeAblativeCrust())
			{
				return;
			}

			AbsorbVictim(player);
			player.Defeat(DisplayName);
		}

        private void SetDeterministicSprite()
        {
            if (_enemyConfig == null || _spriteRenderer == null)
            {
                return;
            }

            Sprite sprite = _enemyConfig.GetSprite(AppearanceSeed);
            if (sprite != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
    }
}