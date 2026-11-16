using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Food : NetworkBehaviour, ICapacity
    {
        private static readonly Color GoldenTint = new(1f, 0.82f, 0.25f);

        private readonly NetworkVariable<float> _valueMultiplier = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [SerializeField] private Color _effectColor = Color.white;

        private const string ViewName = "View";

        private SpriteRenderer _spriteRenderer;
        private Transform _viewTransform;
        private Color _baseColor = Color.white;
        private ulong _dropperObjectId = ulong.MaxValue;
        private float _magnetImmuneUntil;

        public float Capacity { get; set; }
        public float Nutrition { get; private set; }
        public bool IsDropped { get; private set; }

        public int LifecycleVersion { get; private set; }

        public int DensityCell { get; set; } = -1;

        public float ValueMultiplier => _valueMultiplier.Value;

        private bool _isClaimed;

        public bool TryClaim()
        {
            if (_isClaimed)
            {
                return false;
            }

            _isClaimed = true;
            return true;
        }

        public void ResetClaim()
        {
            _isClaimed = false;
        }

        public void PlayEatenEffect()
        {
            if (IsServer && IsSpawned)
            {
                PlayEatenEffectRpc(transform.position, transform.localScale.x);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void PlayEatenEffectRpc(Vector2 position, float size)
        {
            Color color = ValueMultiplier > 1f ? GoldenTint : _effectColor;
            GameVfx.Eat(position, color, size * FoodViewScale.Factor);
        }

        public void SetDropper(ulong dropperObjectId, float magnetImmunitySeconds)
        {
            _dropperObjectId = dropperObjectId;
            _magnetImmuneUntil = Time.time + magnetImmunitySeconds;
        }

        public bool IsMagnetImmuneFor(ulong objectId)
        {
            return _dropperObjectId == objectId && Time.time < _magnetImmuneUntil;
        }

        public void SetNutrition(float nutrition)
        {
            Nutrition = Mathf.Max(0f, nutrition);
        }

        public float GetGrowth(float growthMultiplier)
        {
            return GrowthRules.FoodGrowth(Capacity, growthMultiplier, ValueMultiplier, Nutrition);
        }

        public int MarkAsDropped()
        {
            IsDropped = true;
            return ++LifecycleVersion;
        }

        public void MarkAsStored()
        {
            IsDropped = false;
            Nutrition = 0f;
            DensityCell = -1;
            _dropperObjectId = ulong.MaxValue;
            _magnetImmuneUntil = 0f;
            LifecycleVersion++;
        }

        public void SetValueMultiplier(float value)
        {
            if (IsServer)
            {
                _valueMultiplier.Value = Mathf.Max(1f, value);
            }
        }

        private void Awake()
        {
            if (TryGetComponent(out SpriteRenderer source))
            {
                _baseColor = source.color;
                _spriteRenderer = CreateView(source);
            }
        }

        private void OnEnable()
        {
            FoodViewScale.Changed += ApplyViewScale;
            ApplyViewScale(FoodViewScale.Factor);
        }

        private void OnDisable()
        {
            FoodViewScale.Changed -= ApplyViewScale;
        }

        private SpriteRenderer CreateView(SpriteRenderer source)
        {
            var viewObject = new GameObject(ViewName) { layer = gameObject.layer };
            _viewTransform = viewObject.transform;
            _viewTransform.SetParent(transform, false);

            SpriteRenderer view = viewObject.AddComponent<SpriteRenderer>();
            view.sprite = source.sprite;
            view.color = source.color;
            view.sharedMaterial = source.sharedMaterial;
            view.flipX = source.flipX;
            view.flipY = source.flipY;
            view.sortingLayerID = source.sortingLayerID;
            view.sortingOrder = source.sortingOrder;
            view.maskInteraction = source.maskInteraction;
            source.enabled = false;
            return view;
        }

        private void ApplyViewScale(float factor)
        {
            if (_viewTransform != null)
            {
                _viewTransform.localScale = new Vector3(factor, factor, 1f);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            SceneContainers.Attach(transform, SceneContainers.Food);
            _valueMultiplier.OnValueChanged += OnValueMultiplierChanged;
            ApplyGoldenTint(_valueMultiplier.Value);
        }

        public override void OnNetworkDespawn()
        {
            _valueMultiplier.OnValueChanged -= OnValueMultiplierChanged;
            base.OnNetworkDespawn();
        }

        private void OnValueMultiplierChanged(float _, float value)
        {
            ApplyGoldenTint(value);
        }

        private void ApplyGoldenTint(float value)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = value > 1f ? GoldenTint : _baseColor;
            }
        }
    }
}