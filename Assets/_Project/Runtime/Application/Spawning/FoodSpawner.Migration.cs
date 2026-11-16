using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public sealed partial class FoodSpawner
    {
        private MatchMigrationStore _migration;
        [VContainer.Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;

        public FoodFieldMigrationState CaptureFieldMigration()
        {
            var state = _fieldMap.CaptureMigration();
            state.RelocateTimer = _relocateTimer;
            return state;
        }

        public FoodMigrationState CaptureFoodMigration(Food food)
        {
            var state = food.CaptureMigration();
            if (_cometFood.TryGetValue(food, out float remaining))
            {
                state.CometTrail = true;
                state.Remaining = remaining;
            }
            return state;
        }

        public Food RestoreFoodMigration(FoodMigrationState state)
        {
            Food food = CreateObject(state.Position, state.Scale);
            food.RestoreMigration(state);
            if (state.Dropped)
            {
                _ = ReleaseAfterLifetimeAsync(food, food.LifecycleVersion, state.Remaining);
            }
            else
            {
                _ambientFood.Add(food);
            }
            if (state.CometTrail)
            {
                _cometFood[food] = state.Remaining;
            }
            return food;
        }

        public void RestoreFieldMigration(FoodFieldMigrationState state)
        {
            _fieldMap.RestoreMigration(state);
            _relocateTimer = state.RelocateTimer;
        }

        public ILootDrop RestoreLoot(ulong[] oldIds, IReadOnlyDictionary<ulong, ulong> ids)
        {
            var drop = new LootDrop(this, oldIds.Length);
            var manager = Unity.Netcode.NetworkManager.Singleton;
            foreach (ulong oldId in oldIds)
            {
                if (ids.TryGetValue(oldId, out ulong id) &&
                    manager.SpawnManager.SpawnedObjects.TryGetValue(id, out var obj) &&
                    obj.TryGetComponent(out Food food))
                {
                    drop.Add(food, food.LifecycleVersion);
                }
            }
            return drop;
        }
    }
}
