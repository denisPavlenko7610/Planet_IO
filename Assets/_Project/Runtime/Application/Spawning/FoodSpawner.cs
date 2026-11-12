using RD.Foundation;
using UnityEngine;
using System;
using System.Collections.Generic;
using PlanetIO.Pooling;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public sealed class FoodSpawner : Spawner<Food>, IRespawnService<Food>, ISpawnService<Food>, ILootSpawnService, IFoodFieldGuide
    {
        public const float LootMassFraction = 0.6f;
        private const float LootItemsPerCapacity = 20f;
        private const int MinimumLootItems = 4;
        private const int MaximumLootItems = 30;
        private const float MinimumLootScale = 0.7f;
        private const float MaximumLootScale = 1.6f;
        private const float LootScalePerNutrition = 10f;
        private const float LootTintMultiplier = 2f;
        private const float GoldenValueMultiplier = 8f;

        [SerializeField, Min(0.1f)]
        private float _droppedPointLifetime = 10f;
        [SerializeField, Min(0.1f)]
        private float _lootLifetime = 20f;
        [SerializeField, Range(0f, 1f)]
        private float _goldenChance = 0.04f;
        [SerializeField]
        private FoodFieldSettings _fieldSettings = new();
        [SerializeField]
        private FoodSizeTiers _sizeTiers = new();

        private FoodFieldMap _fieldMap;
        private float _relocateTimer;
        private int _sourceCell = FoodFieldMap.NoCell;
        private int _pendingCell = FoodFieldMap.NoCell;
        private bool _pendingGolden;

        public FoodFieldMap FieldMap => _fieldMap;

        public override void Initialize(ObjectPool<Food> objectPool)
        {
            base.Initialize(objectPool);
            _fieldMap = new FoodFieldMap(
                SpawnArea,
                objectPool.Capacity,
                _fieldSettings,
                Random.Range(int.MinValue, int.MaxValue));
            _relocateTimer = 0f;
        }

        public void SpawnAt(Transform spawnTransform, float nutrition)
        {
            if (spawnTransform == null)
            {
                throw new ArgumentNullException(nameof(spawnTransform));
            }

            Food point = CreateObject(spawnTransform.position, _sizeTiers.ResolveDropped(Random.value));

            int lifecycleVersion = point.MarkAsDropped();
            point.SetNutrition(nutrition);
            point.SetValueMultiplier(1f);

            _ = ReleaseAfterLifetimeAsync(point, lifecycleVersion, _droppedPointLifetime);
        }

        public ILootDrop SpawnLoot(Vector2 position, float sourceCapacity)
        {
            int itemCount = GetLootItemCount(sourceCapacity);
            LootDrop drop = new(this, itemCount);
            float itemNutrition = sourceCapacity * LootMassFraction / itemCount;
            float itemScale = Mathf.Clamp(
                MinimumLootScale + itemNutrition * LootScalePerNutrition, MinimumLootScale, MaximumLootScale);
            float spreadRadius = Mathf.Max(sourceCapacity * 1.5f, 0.6f);
            float startAngle = Random.Range(0f, 360f / itemCount);

            for (int index = 0; index < itemCount; index++)
            {
                float angle = (startAngle + index * (360f / itemCount)) * Mathf.Deg2Rad;
                float distance = spreadRadius * Random.Range(0.5f, 1.2f);
                Vector2 itemPosition = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                itemPosition = Constants.ClampToWorld(itemPosition);

                Food loot = CreateObject(itemPosition, itemScale);

                int lifecycleVersion = loot.MarkAsDropped();
                loot.SetNutrition(itemNutrition);
                loot.SetValueMultiplier(LootTintMultiplier);

                drop.Add(loot, lifecycleVersion);
                _ = ReleaseAfterLifetimeAsync(loot, lifecycleVersion, _lootLifetime);
            }

            return drop;
        }

        public static int GetLootItemCount(float sourceCapacity)
        {
            return Mathf.Clamp(
                Mathf.RoundToInt(sourceCapacity * LootItemsPerCapacity), MinimumLootItems, MaximumLootItems);
        }

        public bool TryGetFieldTarget(Vector2 from, float minimumDistance, out Vector2 target)
        {
            if (_fieldMap == null)
            {
                target = default;
                return false;
            }

            return _fieldMap.TryGetFieldTarget(from, minimumDistance, out target);
        }

        public void Respawn(Food point)
        {
            if (point == null)
            {
                return;
            }

            if (point.IsDropped)
            {
                ReturnDroppedPoint(point);
                return;
            }

            _sourceCell = point.DensityCell;
            _fieldMap?.Release(_sourceCell);
            point.DensityCell = FoodFieldMap.NoCell;

            RespawnObject(point);
            point.ResetClaim();

            _sourceCell = FoodFieldMap.NoCell;
        }

        protected override Vector2 GetRandomPosition()
        {
            if (_fieldMap == null)
            {
                return base.GetRandomPosition();
            }

            _pendingCell = _fieldMap.Reserve(_sourceCell);
            return _fieldMap.SamplePosition(_pendingCell);
        }

        protected override float GetRandomScale()
        {
            _pendingGolden = Random.value < _goldenChance;
            return _pendingGolden
                ? _sizeTiers.ResolveGolden(Random.value)
                : _sizeTiers.Resolve(Random.value, Random.value);
        }

        protected override void OnRandomPlaced(Food placedObject)
        {
            placedObject.DensityCell = _pendingCell;
            placedObject.SetValueMultiplier(_pendingGolden ? GoldenValueMultiplier : 1f);
            _pendingCell = FoodFieldMap.NoCell;
            _pendingGolden = false;
        }

        private void Update()
        {
            if (_fieldMap == null || _fieldSettings.RelocateInterval <= 0f)
            {
                return;
            }

            _relocateTimer += Time.deltaTime;
            if (_relocateTimer < _fieldSettings.RelocateInterval)
            {
                return;
            }

            _relocateTimer = 0f;
            _fieldMap.RelocateField();
        }

        private void ReturnDroppedPoint(Food point)
        {
            point.MarkAsStored();
            ReleaseObject(point);
        }

        private sealed class LootDrop : ILootDrop
        {
            private readonly FoodSpawner _spawner;
            private readonly List<(Food Food, int LifecycleVersion)> _items;

            public LootDrop(FoodSpawner spawner, int capacity)
            {
                _spawner = spawner;
                _items = new List<(Food, int)>(capacity);
            }

            public void Add(Food food, int lifecycleVersion)
            {
                _items.Add((food, lifecycleVersion));
            }

            public void Discard()
            {
                foreach ((Food food, int lifecycleVersion) in _items)
                {
                    if (food != null && food.IsDropped && food.LifecycleVersion == lifecycleVersion)
                    {
                        _spawner.ReturnDroppedPoint(food);
                    }
                }

                _items.Clear();
            }
        }

        private async Awaitable ReleaseAfterLifetimeAsync(Food point, int lifecycleVersion, float lifetime)
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(lifetime, destroyCancellationToken);

                if (point == null ||
                    !point.IsDropped ||
                    point.LifecycleVersion != lifecycleVersion)
                {
                    return;
                }

                ReturnDroppedPoint(point);
            }
            catch (OperationCanceledException)
            {
                UnityLogger.Log("Dropped point lifetime cancelled.");
            }
        }
    }
}
