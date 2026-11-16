using System.Collections.Generic;
using System.Linq;

namespace PlanetIO.Application
{
    public sealed partial class WorldDirector
    {
        private MatchMigrationStore _migrationStore;
        private readonly Dictionary<ulong, double> _waitingPortals = new();
        private readonly HashSet<ulong> _waitingVictims = new();
        public bool IsMigratingMeteor(CometMovement comet) => _launched.Contains(comet);
        public void AddMigratedMeteor(CometMovement comet) => _launched.Add(comet);

        public WorldMigrationState CaptureMigration(MatchMigrationStore store)
        {
            var portals = _portalReady.ToDictionary(
                p => store.StableId(p.Key),
                p => System.Math.Max(0, p.Value - OrbitClock.Now)
            );
            foreach (var p in _waitingPortals)
            {
                portals[p.Key] = System.Math.Max(0, p.Value - OrbitClock.Now);
            }
            return new WorldMigrationState
            {
                Seed = _seed, Modifier = (int)Modifier, Phase = (int)_phase,
                ActiveIndex = _activeIndex, AbsorbedMass = _absorbedMass,
                BlackHoleVictims =
                    _blackHoleVictims.Select(store.StableId).Concat(_waitingVictims).Distinct().ToArray(),
                PortalIds = portals.Keys.ToArray(), PortalRemaining = portals.Values.ToArray()
            };
        }

        public void RestoreMigration(WorldMigrationState s, MatchMigrationStore store)
        {
            _migrationStore = store;
            DestroyVisuals();
            MapFeatureField.Reset();
            _seed = s.Seed;
            Modifier = (DailyModifier)s.Modifier;
            _lootScale = DailyModifierRules.LootScale(Modifier);
            MapFeatureField.GravityScale = DailyModifierRules.GravityScale(Modifier);
            BuildMap();
            _phase = (Phase)s.Phase;
            _activeIndex = s.ActiveIndex;
            _absorbedMass = s.AbsorbedMass;
            if (_activeIndex >= 0)
            {
                _event = CosmicEventRules.Get(_seed, _activeIndex);
            }
            _portalReady.Clear();
            _blackHoleVictims.Clear();
            _waitingPortals.Clear();
            _waitingVictims.Clear();
            for (int i = 0; i < s.PortalIds.Length; i++)
            {
                _waitingPortals[s.PortalIds[i]] = OrbitClock.Now + s.PortalRemaining[i];
            }
            foreach (ulong id in s.BlackHoleVictims)
            {
                _waitingVictims.Add(id);
            }
            ApplyMigratedEntityTimers();
            UpdateAnchor(OrbitClock.Now);
        }

        private void ApplyMigratedEntityTimers()
        {
            if (_migrationStore == null)
            {
                return;
            }

            foreach (ulong id in _waitingPortals.Keys.ToArray())
            {
                if (_migrationStore.ObjectIds.TryGetValue(id, out ulong networkId))
                {
                    _portalReady[networkId] = _waitingPortals[id];
                    _waitingPortals.Remove(id);
                }
            }
            foreach (ulong id in _waitingVictims.ToArray())
            {
                if (_migrationStore.ObjectIds.TryGetValue(id, out ulong networkId))
                {
                    _blackHoleVictims.Add(networkId);
                    _waitingVictims.Remove(id);
                }
            }
        }
    }
}
