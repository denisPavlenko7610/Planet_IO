using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace PlanetIO
{
	public sealed class FoodSpawner : MonoBehaviour, IFoodService, ILootSpawnService, IFoodFieldGuide, ICometFoodTrail
	{
		public const float LootMassFraction = CombatRules.LootMassFraction;
		private const float GoldenValueMultiplier = 8f;
		[SerializeField] private float _droppedPointLifetime = 10f;
		[SerializeField] private float _lootLifetime = 20f;
		[SerializeField] private float _goldenChance = 0.04f;
		[SerializeField] private FoodFieldSettings _fieldSettings = new();
		[SerializeField] private FoodSizeTiers _sizeTiers = new();
		[SerializeField] private Vector2 _horizontalSpawnRange = new(-210f, 210f);
		[SerializeField] private Vector2 _verticalSpawnRange = new(-125f, 150f);
		private FoodFieldMap _fieldMap;
		private float _relocateTimer;
		private readonly List<FoodRecord> _ambientFood = new(2000);
		private readonly Dictionary<FoodRecord, float> _cometFood = new();
		private readonly ExpiryQueue<ulong> _expiry = new();
		private IObjectResolver _resolver;
		private IGameStateService _gameState;
		private FoodReplication _replication;
		private FoodWorld _world;
		private FoodViewRenderer _view;
		private NetworkManager _network;
		public FoodWorld World => _world?.Authoritative == true ? _world : _replication?.Replica.World ?? _world;
		public FoodWorld ViewWorld => _world?.Authoritative == true ? _world : _replication?.Replica.World;
		public bool IsReplicaReady => _world?.Authoritative == true || _replication?.Replica.Ready == true;
		public event Action<FoodEatEvent> Eaten;
		public void MarkWorldReady() => _replication.MarkReady();

		[Inject] public void ConstructCometTrail(IObjectResolver resolver) => _resolver = resolver;

		public void Initialize(FoodPool configuration, NetworkManager network = null)
		{
			if (_world != null) return;
			NetworkManager manager = network != null ? network : NetworkManager.Singleton;
			_network = manager;
			_world = new FoodWorld(manager != null && manager.IsServer, WorldBounds.Area);
			if (_world.Authoritative)
				_fieldMap = new FoodFieldMap(Rect.MinMaxRect(_horizontalSpawnRange.x, _verticalSpawnRange.x,
					_horizontalSpawnRange.y, _verticalSpawnRange.y), configuration.Capacity, _fieldSettings, Random.Range(int.MinValue, int.MaxValue));
			_replication = new FoodReplication(this, manager);
			if (manager.IsClient && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
				_view = new FoodViewRenderer(configuration, SceneContainers.Get(SceneContainers.Food));
		}

		public FoodRecord CreateObject()
		{
			_world.RequireAuthority();
			int cell = _fieldMap.Reserve(FoodFieldMap.NoCell);
			bool golden = Random.value < _goldenChance;
			float size = golden ? _sizeTiers.ResolveGolden(Random.value) : _sizeTiers.Resolve(Random.value, Random.value);
			FoodRecord food = _world.Create(_fieldMap.SamplePosition(cell), size, (byte)Random.Range(0, FoodPool.Colors.Length));
			_world.Configure(food, false, false, cell, 0f, golden ? GoldenValueMultiplier : 1f);
			_ambientFood.Add(food);
			return food;
		}

		public FoodRecord SpawnAt(Transform position, float nutrition)
		{
			if (position == null) throw new ArgumentNullException(nameof(position));
			FoodRecord food = _world.Create(WorldBounds.Clamp(position.position), _sizeTiers.ResolveDropped(Random.value), (byte)Random.Range(0, FoodPool.Colors.Length));
			_world.Configure(food, true, false, -1, nutrition, 1f, _droppedPointLifetime);
			_expiry.Enqueue(food.Id, 0, food.ExpiresAt);
			return food;
		}

		public ILootDrop SpawnLoot(Vector2 position, float sourceCapacity)
		{
			int count = GetLootItemCount(sourceCapacity);
			var drop = new LootDrop(this, count);
			float nutrition = sourceCapacity * LootMassFraction / count;
			float scale = Mathf.Clamp(0.7f + nutrition * 10f, 0.7f, 1.6f);
			float spread = Mathf.Max(sourceCapacity * 1.5f, 0.6f), start = Random.Range(0f, 360f / count);
			for (int index = 0; index < count; index++)
			{
				float angle = (start + index * (360f / count)) * Mathf.Deg2Rad;
				Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (spread * Random.Range(0.5f, 1.2f));
				FoodRecord food = _world.Create(WorldBounds.Clamp(position + offset), scale, (byte)Random.Range(0, FoodPool.Colors.Length));
				_world.Configure(food, true, false, -1, nutrition, 2f, _lootLifetime);
				drop.Add(food);
				_expiry.Enqueue(food.Id, 0, food.ExpiresAt);
			}
			return drop;
		}

		public static int GetLootItemCount(float capacity) => Mathf.Clamp(Mathf.RoundToInt(capacity * 20f), 4, 30);
		public void Query(Vector2 position, float radius, List<FoodRecord> result) => _world.Query(position, radius, result);
		public bool TryGetFieldTarget(Vector2 from, float distance, out Vector2 target)
		{
			if (_fieldMap != null) return _fieldMap.TryGetFieldTarget(from, distance, out target);
			target = default;
			return false;
		}

		public void Respawn(FoodRecord food)
		{
			if (food == null || !food.Active) return;
			if (food.IsDropped) { _world.Remove(food); return; }
			_cometFood.Remove(food);
			int previous = food.DensityCell;
			_fieldMap.Release(previous);
			int cell = _fieldMap.Reserve(previous);
			bool golden = Random.value < _goldenChance;
			_world.Renew(food);
			food.Position = _fieldMap.SamplePosition(cell);
			food.Capacity = golden ? _sizeTiers.ResolveGolden(Random.value) : _sizeTiers.Resolve(Random.value, Random.value);
			_world.Configure(food, false, false, cell, 0f, golden ? GoldenValueMultiplier : 1f);
		}

		public bool TryLeaveCometFood(Vector2 position)
		{
			if (!CometRules.CanLeaveFood(_cometFood.Count, position) || _ambientFood.Count == 0) return false;
			for (int attempt = 0; attempt < 24; attempt++)
			{
				FoodRecord food = _ambientFood[Random.Range(0, _ambientFood.Count)];
				if (!food.Active || food.IsDropped || food.ValueMultiplier > 1f || food.DensityCell == -1 || (food.Position - position).sqrMagnitude < 4f) continue;
				_fieldMap.Release(food.DensityCell);
				food.Position = position;
				_world.Configure(food, false, true, -1, food.Nutrition, food.ValueMultiplier, CometRules.FoodLifetime);
				_cometFood.Add(food, CometRules.FoodLifetime);
				_expiry.Enqueue(food.Id, 0, food.ExpiresAt);
				return true;
			}
			return false;
		}

		public void PlayEatenEffect(FoodRecord food)
		{
			_world.RequireAuthority();
			var effect = new FoodEatEvent(food.Position, food.Capacity, food.Flavor, food.ValueMultiplier > 1f);
			Eaten?.Invoke(effect);
			if (_network.IsClient) ShowEffect(effect);
		}

		internal static void ShowEffect(FoodEatEvent effect)
		{
			if (!ViewBounds.IsVisible(effect.Position, 1f)) return;
			Color color = effect.Golden ? new Color(1f, 0.82f, 0.25f) : FoodPool.Colors[effect.Flavor % FoodPool.Colors.Length];
			GameVfx.Eat(effect.Position, color, effect.Size * FoodViewScale.Factor);
		}

		private void Update()
		{
			if (_world == null) return;
			if (_world.Authoritative)
			{
				if (_resolver != null) _gameState ??= _resolver.Resolve<IGameStateService>();
				if (_gameState?.IsGameplayActive == true)
				{
					_world.Advance(Time.deltaTime);
					while (_expiry.TryDequeueExpired(_world.Clock, out ulong id, out _))
					{
						FoodRecord food = _world.Find(id);
						if (food != null) Respawn(food);
					}
					_relocateTimer += Time.deltaTime;
					if (_fieldSettings.RelocateInterval > 0f && _relocateTimer >= _fieldSettings.RelocateInterval)
					{ _relocateTimer = 0f; _fieldMap.RelocateField(); }
				}
			}
			_replication.Tick(Time.unscaledDeltaTime);
		}

		private void LateUpdate() => _view?.Render(ViewWorld, Time.deltaTime);
		private void OnDestroy() { _replication?.Dispose(); _view?.Dispose(); _expiry.Clear(); }

		private sealed class LootDrop : ILootDrop
		{
			private readonly FoodSpawner _spawner;
			private readonly List<ulong> _ids;
			public LootDrop(FoodSpawner spawner, int count) { _spawner = spawner; _ids = new List<ulong>(count); }
			public void Add(FoodRecord food) => _ids.Add(food.Id);
			public void Discard()
			{
				foreach (ulong id in _ids) { FoodRecord food = _spawner.World.Find(id); if (food?.IsDropped == true) _spawner.World.Remove(food); }
				_ids.Clear();
			}
		}
	}
}
