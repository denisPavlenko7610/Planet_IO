using System;
using RD.Settings;
using VContainer;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace PlanetIO
{
    public abstract partial class PlanetScale : NetworkBehaviour, ICapacity
    {
        private const float MinAllowedCapacity = 0.01f;
        private const float ScaleDepth = 1f;
        private const int DisplayNameMaximumLength = 32;

        private static readonly char[] RichTextCharacters = { '<', '>' };

        [Header("Capacity")]
        [FormerlySerializedAs("_minimumCapacity")]
        [SerializeField, Min(MinAllowedCapacity)] private float _minCapacity = 0.08f;
        [FormerlySerializedAs("_maximumCapacity")]
        [SerializeField, Min(MinAllowedCapacity)] private float _maxCapacity = PlanetSizeRules.MaximumCapacity;
        [SerializeField, Min(MinAllowedCapacity)] private float _initialCapacity = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _killAbsorbFraction = 0.4f;
        [SerializeField, Min(1f)] private float _eatSizeRatio = CombatRules.DefaultEatSizeRatio;
        [SerializeField, Min(0f)] private float _foodGrowthFalloff = 0.5f;

        private readonly NetworkVariable<float> _networkCapacity = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly NetworkVariable<FixedString64Bytes> _networkDisplayName = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

		private float _targetScale;
        private float _localCapacity;
		private float _cometHealCooldown;
        private ISettingsService _difficultySettings;
        private INetworkSessionService _difficultySession;
        protected NetworkSessionMode DifficultySessionMode => _difficultySession?.Mode ?? NetworkSessionMode.None;
        public BotDifficulty DifficultyLevel => BotDifficultyRules.Resolve(
            _difficultySettings?.Get(GameSettingKeys.BotDifficulty) ?? 1, DifficultySessionMode,
            _difficultySession?.CurrentRoom.Difficulty ?? BotDifficulty.Medium);
        protected virtual float IncomingDamageMultiplier => 1f;

        [Inject]
        public void ConstructDifficultySettings(ISettingsService settings, INetworkSessionService session)
        {
            _difficultySettings = settings;
            _difficultySession = session;
        }

        public float MinCapacity => _minCapacity;
        public float InitialCapacity => _initialCapacity;

        public float Capacity
        {
            get => IsSpawned ? _networkCapacity.Value : _localCapacity;
            set => TrySetCapacity(value);
        }

        public string DisplayName =>
            _networkDisplayName.Value.Length > 0
                ? _networkDisplayName.Value.ToString()
                : GetFallbackDisplayName();

        public event Action<float> CapacityChanged;
        public event Action<float, float> Damaged;
        public event Action<string> DisplayNameChanged;

        protected virtual void Awake()
        {
            NormalizeCapacityBounds();
            ApplyCapacity(_minCapacity, false);
            Capacity = _initialCapacity;
            InitializeMoons();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                _networkCapacity.Value = ClampCapacity(_localCapacity);
            }

            _networkCapacity.OnValueChanged += OnNetworkCapacityChanged;
            _networkDisplayName.OnValueChanged += OnDisplayNameValueChanged;
            SubscribeMoons();
            PlanetDirectory.Register(this);
            ApplyCapacity(_networkCapacity.Value, true);
			transform.localScale = new Vector3(_targetScale, _targetScale, ScaleDepth);
            AttachMoonViews();
        }

        public override void OnNetworkDespawn()
        {
            float lastCapacity = _networkCapacity.Value;
            _networkCapacity.OnValueChanged -= OnNetworkCapacityChanged;
            _networkDisplayName.OnValueChanged -= OnDisplayNameValueChanged;
            UnsubscribeMoons();
            PlanetDirectory.Unregister(this);
            _localCapacity = ClampCapacity(lastCapacity);
            base.OnNetworkDespawn();
        }

        protected abstract float FoodGrowthMultiplier { get; }
        protected abstract float CometDamageMultiplier { get; }
        protected abstract string GetFallbackDisplayName();
        protected IFoodService FoodRespawnService { get; set; }
        protected IRespawnService<Comet> CometRespawnService { get; set; }

        protected void SetDisplayName(string value)
        {
            if (!IsServer)
            {
                return;
            }

            string normalized = (value ?? string.Empty).Trim();
            if (normalized.Length > DisplayNameMaximumLength)
            {
                normalized = normalized[..DisplayNameMaximumLength];
            }

            if (normalized.IndexOfAny(RichTextCharacters) >= 0)
            {
                normalized = normalized.Replace("<", string.Empty).Replace(">", string.Empty);
            }

            _networkDisplayName.Value = normalized;
        }

        protected bool Grow(float amount)
        {
            return ChangeCapacity(Mathf.Abs(amount));
        }

        protected bool Shrink(float amount)
        {
            return ChangeCapacity(-Mathf.Abs(amount));
        }

        protected bool TakeDamage(float amount)
        {
            if (!IsServer || !IsSpawned || amount <= 0f || !float.IsFinite(amount))
			{
				return false;
			}
			float before = Capacity;
            float lost = before - ClampCapacity(before - amount);
            bool changed = Shrink(amount);
            if (lost > 0f)
			{
				ReportDamageRpc(lost, before);
			}
			return changed;
        }

        [Rpc(SendTo.Owner)]
        private void ReportDamageRpc(float lost, float before)
        {
            Damaged?.Invoke(lost, before);
        }

        public bool CanDevour(PlanetScale victim)
        {
            return victim != null && CombatRules.CanDevour(Capacity, victim.Capacity, _eatSizeRatio);
        }

		internal void DropBoostFood(ISpawnService<FoodRecord> spawner, Transform point, Vector2 travelDirection, float nutrition)
		{
			point.position = Constants.ClampToWorld(
				BoostDropRules.SpawnPosition(transform.position, travelDirection, BodyRadius));
			spawner.SpawnAt(point, nutrition)?.SetDropper(NetworkObjectId, FoodMagnetRules.OwnDropGraceSeconds);
		}

        protected virtual void DeathCheck(float capacity)
        {
        }

		protected void ClearCometHealCooldown()
		{
			_cometHealCooldown = 0f;
		}

        protected void HandleEntityCollision(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            if (other.TryGetComponent(out Comet comet))
            {
                float cometMass = CometRules.Damage(Capacity, CometDamageMultiplier);
                if (CometHealFraction > 0f)
                {
                    if (_cometHealCooldown <= 0f)
                    {
                        Grow(cometMass * CometHealFraction);
                        _cometHealCooldown = PerkRules.CometHealCooldownSeconds;
                    }
                }
                else
                {
                    TakeDamage(cometMass * IncomingDamageMultiplier);
                }

                comet.PlayImpactEffect();
                CometRespawnService?.Respawn(comet);
            }
        }

		public bool TryEatFood(FoodRecord food)
		{
			if (!IsServer || !IsCombatActive || food == null || food.IsMagnetImmuneFor(NetworkObjectId) || !food.TryClaim()) return false;
			Grow(GrowthRules.ApplyFalloff(food.GetGrowth(FoodGrowthMultiplier), Capacity, _foodGrowthFalloff, food.Nutrition > 0f));
			FoodRespawnService.PlayEatenEffect(food);
			FoodRespawnService.Respawn(food);
			return true;
		}

        private bool ChangeCapacity(float delta)
        {
            return TrySetCapacity(Capacity + delta);
        }

        private bool TrySetCapacity(float value)
        {
            if (float.IsNaN(value) || IsSpawned && !IsServer)
            {
                return false;
            }

            float clamped = ClampCapacity(value);
            if (Capacity == clamped)
            {
                if (value < clamped)
                {
                    DeathCheck(clamped);
                }

                return false;
            }

            if (IsSpawned)
            {
                _networkCapacity.Value = clamped;
            }
            else
            {
                ApplyCapacity(clamped, true);
            }

            return true;
        }

        private void OnNetworkCapacityChanged(float _, float current)
        {
            ApplyCapacity(current, true);
        }

        private void OnDisplayNameValueChanged(FixedString64Bytes _, FixedString64Bytes value)
        {
            DisplayNameChanged?.Invoke(value.ToString());
        }

        private void ApplyCapacity(float capacity, bool notify)
        {
            float previousCapacity = _localCapacity;
            _localCapacity = capacity;
			_targetScale = PlanetSizeRules.Scale(capacity);
			if (!IsSpawned || capacity <= previousCapacity)
			{
				transform.localScale = new Vector3(_targetScale, _targetScale, ScaleDepth);
			}

            if (notify)
            {
                RefreshTierServer(capacity);
                CapacityChanged?.Invoke(capacity);
                if (capacity < previousCapacity)
                {
                    DeathCheck(capacity);
                }
            }
        }

		protected virtual void LateUpdate()
		{
			if (!IsSpawned) return;
			float scale = PlanetSizeRules.SmoothGrowth(transform.localScale.x, _targetScale, Time.deltaTime);
			transform.localScale = new Vector3(scale, scale, ScaleDepth);
		}

        private float ClampCapacity(float value)
        {
            return Mathf.Clamp(value, _minCapacity, _maxCapacity);
        }

        private void NormalizeCapacityBounds()
        {
            _minCapacity = float.IsFinite(_minCapacity)
                ? Mathf.Max(MinAllowedCapacity, _minCapacity)
                : MinAllowedCapacity;
            _maxCapacity = float.IsFinite(_maxCapacity)
                ? Mathf.Max(_minCapacity, _maxCapacity)
                : _minCapacity;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            NormalizeCapacityBounds();
        }
#endif
    }
}