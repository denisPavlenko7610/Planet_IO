using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Application
{
    public sealed class MigrationWorldService : IDisposable
    {
        private readonly MatchMigrationStore _store;
        private readonly NetworkManager _network;
        private readonly FoodSpawner _food;
        private readonly EnemySpawner _bots;
        private readonly CometSpawner _comets;
        private readonly IObjectResolver _resolver;
        private bool _ready;

        public MigrationWorldService(
            MatchMigrationStore store,
            NetworkManager network,
            FoodSpawner food,
            EnemySpawner bots,
            CometSpawner comets,
            IObjectResolver resolver
        )
        {
            _store = store;
            _network = network;
            _food = food;
            _bots = bots;
            _comets = comets;
            _resolver = resolver;
            _store.CaptureWorld = Capture;
        }

        public void MarkReady() => _ready = true;

        private PlanetMigrationState CapturePlanet(PlanetScale planet)
        {
            var state = planet.CaptureMigration();
            state.ObjectId = _store.StableId(planet.NetworkObjectId);
            if (state.TargetId != ulong.MaxValue)
            {
                state.TargetId = _store.StableId(state.TargetId);
            }
            return state;
        }

		public MatchSnapshot Capture()
		{
			if (!_ready || !_network.IsServer)
			{
				return null;
			}

			WorldDirector world = ResolveWorld();
			List<Player> players = FindSpawned<Player>();
			List<Enemy> bots = FindSpawned<Enemy>();
			List<CometMovement> comets = FindSpawned<CometMovement>();

			List<object> live = new(players.Count + bots.Count + comets.Count);
			live.AddRange(players);
			live.AddRange(bots);
			live.AddRange(comets);
			_store.PruneTracked(live);

			TrackAll(players);
			TrackAll(bots);
			TrackAll(comets);

			MatchSnapshot snapshot = new()
			{
				Ready = true,
				Clock = OrbitClock.Now,
				Difficulty = _resolver.Resolve<INetworkSessionService>().CurrentRoom.Difficulty,
				Fields = _food.CaptureFieldMigration(),
				FoodEpoch = _food.Epoch
			};
			snapshot.Players = CapturePlanets(players);
			snapshot.Bots = CapturePlanets(bots);
			snapshot.Food = CaptureFood();
			snapshot.Comets = CaptureComets(comets, world);
			snapshot.World = world.CaptureMigration(_store);

			return snapshot;
		}

		private WorldDirector ResolveWorld() => _resolver.Resolve<IWorldEvents>() as WorldDirector;

		private static List<T> FindSpawned<T>() where T : NetworkBehaviour
		{
			T[] found = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
			List<T> spawned = new(found.Length);

			foreach (T item in found)
			{
				if (item.IsSpawned)
				{
					spawned.Add(item);
				}
			}

			return spawned;
		}

		private void TrackAll<T>(List<T> items) where T : NetworkBehaviour
		{
			foreach (T item in items)
			{
				_store.TrackObject(item.NetworkObjectId, item);
			}
		}

		private PlanetMigrationState[] CapturePlanets<T>(List<T> planets) where T : PlanetScale
		{
			PlanetMigrationState[] states = new PlanetMigrationState[planets.Count];

			for (int index = 0; index < states.Length; index++)
			{
				states[index] = CapturePlanet(planets[index]);
			}

			return states;
		}

		private FoodMigrationState[] CaptureFood()
		{
			FoodWorld table = _food.World;
			int used = table.UsedSlots;
			List<FoodMigrationState> states = new(used);

			for (int index = 0; index < used; index++)
			{
				FoodRecord food = table.At(index);
				if (food == null)
				{
					continue;
				}

				FoodMigrationState state = _food.CaptureFoodMigration(food);
				state.DropperId = _store.StableId(state.DropperId);
				states.Add(state);
			}

			return states.ToArray();
		}

		private CometMigrationState[] CaptureComets(List<CometMovement> comets, WorldDirector world)
		{
			CometMigrationState[] states = new CometMigrationState[comets.Count];

			for (int index = 0; index < states.Length; index++)
			{
				CometMovement comet = comets[index];
				CometMigrationState state = comet.CaptureMigration();
				state.Meteor = world.IsMigratingMeteor(comet);
				state.ObjectId = _store.StableId(comet.NetworkObjectId);
				states[index] = state;
			}

			return states;
		}

        public async Awaitable RestoreAsync(NetworkWorldReadyState ready, CancellationToken cancellation)
        {
            var snapshot = _store.Pending ?? throw new InvalidOperationException("Missing migration world.");
            await Awaitable.NextFrameAsync(cancellation);
            for (int i = 0; i < snapshot.Food.Length; i++)
            {
                cancellation.ThrowIfCancellationRequested();
                var state = snapshot.Food[i];
				_food.RestoreFoodMigration(state);
                if (i % 40 == 39)
                {
                    await Awaitable.NextFrameAsync(cancellation);
                }
            }
            _food.RestoreFieldMigration(snapshot.Fields);
            foreach (var state in snapshot.Bots)
            {
                var bot = _bots.CreateObject(state.Position, state.Mass);
                bot.RestoreMigration(state);
                _store.BindObject(state.ObjectId, bot.NetworkObjectId, bot);
            }
			WorldDirector world = ResolveWorld();
            foreach (var state in snapshot.Comets)
            {
                var comet = _comets.CreateObject(state.Position, state.Mass);
                var movement = comet.GetComponent<CometMovement>();
                movement.RestoreMigration(state);
                _store.BindObject(state.ObjectId, comet.NetworkObjectId, movement);
                if (state.Meteor)
                {
                    world.AddMigratedMeteor(movement);
                }
            }
			_food.RestoreDroppers(_store.ObjectIds);
            ready.RestoreClock(snapshot.Clock);
            world.RestoreMigration(snapshot.World, _store);
            _ready = true;
            _store.WorldRestored();
        }

        public void Dispose()
        {
            if (_store.CaptureWorld == Capture)
            {
                _store.CaptureWorld = null;
            }
        }
    }
}
