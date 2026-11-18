using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
	public sealed partial class FoodSpawner
	{
		private MatchMigrationStore _migration;
		private readonly List<FoodMigrationState> _restored = new();
		[Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;
		public FoodFieldMigrationState CaptureFieldMigration()
		{
			FoodFieldMigrationState state = _fieldMap.CaptureMigration();
			state.RelocateTimer = _relocateTimer;
			return state;
		}
		public FoodMigrationState CaptureFoodMigration(FoodRecord food) => _world.Capture(food);
		public FoodRecord RestoreFoodMigration(FoodMigrationState state)
		{
			FoodRecord food = _world.Restore(state);
			if (!food.Active) return food;
			_restored.Add(state);
			if (food.IsDropped || food.IsCometTrail) _expiry.Enqueue(food.Id, 0, food.ExpiresAt);
			if (!food.IsDropped) _ambientFood.Add(food);
			if (food.IsCometTrail) _cometFood[food] = state.Remaining;
			return food;
		}
		public void RestoreFieldMigration(FoodFieldMigrationState state)
		{
			_fieldMap.RestoreMigration(state);
			_relocateTimer = state.RelocateTimer;
			_world.RebuildFreeSlots();
			_replication.StartEpoch(_migration.Pending.FoodEpoch + 1);
		}
		public void RestoreDroppers(IReadOnlyDictionary<ulong, ulong> ids)
		{
			foreach (FoodMigrationState state in _restored)
			{
				FoodRecord food = _world.Find(state.ObjectId);
				if (food != null && ids.TryGetValue(state.DropperId, out ulong id))
					food.SetDropper(id, Mathf.Max(0f, state.Immunity - _world.Clock));
			}
		}
		public ILootDrop RestoreLoot(ulong[] oldIds, IReadOnlyDictionary<ulong, ulong> ids)
		{
			var drop = new LootDrop(this, oldIds.Length);
			foreach (ulong id in oldIds) { FoodRecord food = _world.Find(id); if (food?.IsDropped == true) drop.Add(food); }
			return drop;
		}
	}
}
