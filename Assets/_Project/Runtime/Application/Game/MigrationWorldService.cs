using System;
using System.Linq;
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

            var players = Object.FindObjectsByType<Player>(FindObjectsInactive.Exclude).Where(p => p.IsSpawned)
                .ToArray();
            var bots = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).Where(p => p.IsSpawned).ToArray();
			var foods = Enumerable.Range(0, _food.World.UsedSlots).Select(_food.World.At).Where(p => p != null).ToArray();
            var comets = Object.FindObjectsByType<CometMovement>(FindObjectsInactive.Exclude).Where(p => p.IsSpawned)
                .ToArray();
            _store.PruneTracked(players.Cast<object>().Concat(bots).Concat(comets));
            foreach (var p in players)
            {
                _store.TrackObject(p.NetworkObjectId, p);
            }
            foreach (var p in bots)
            {
                _store.TrackObject(p.NetworkObjectId, p);
            }
            foreach (var p in comets)
            {
                _store.TrackObject(p.NetworkObjectId, p);
            }
            var snapshot = new MatchSnapshot
            {
                Ready = true, Clock = OrbitClock.Now,
                Difficulty = _resolver.Resolve<INetworkSessionService>().CurrentRoom.Difficulty,
                Fields = _food.CaptureFieldMigration(), FoodEpoch = _food.Epoch
            };
            snapshot.Players = players.Select(CapturePlanet).ToArray();
            snapshot.Bots = bots.Select(CapturePlanet).ToArray();
            snapshot.Food = foods.Select(food =>
                {
                    var s = _food.CaptureFoodMigration(food);
                    s.DropperId = _store.StableId(s.DropperId);
                    return s;
                }
            ).ToArray();
            var world = _resolver.Resolve<IWorldEvents>() as WorldDirector;
            snapshot.Comets = comets.Select(comet =>
                {
                    var s = comet.CaptureMigration();
                    s.Meteor = world != null && world.IsMigratingMeteor(comet);
                    s.ObjectId = _store.StableId(comet.NetworkObjectId);
                    return s;
                }
            ).ToArray();
            snapshot.World = world.CaptureMigration(_store);
            return snapshot;
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
            var world = _resolver.Resolve<IWorldEvents>() as WorldDirector;
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
