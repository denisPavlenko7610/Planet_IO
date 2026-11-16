using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlanetIO
{
    public sealed class MatchMigrationStore
    {
        public const float SnapshotIntervalSeconds = 2f;

        public string MatchId { get; private set; }
        public string LocalIdentity { get; set; } = "solo";
        public bool IsRecovering { get; private set; }
        public bool HasPendingWorld => Pending != null;
        public MatchSnapshot Pending { get; private set; }
        public string Failure { get; private set; }
        public FoodMigrationState[] RestoredFood { get; private set; } = Array.Empty<FoodMigrationState>();
        public Func<MatchSnapshot> CaptureWorld;
        public Func<UnityEngine.Awaitable> PausePeers;
        public event Action RecoveryStarted;
        public event Action RecoveryCompleted;
        public readonly Dictionary<ulong, string> Clients = new();
        public readonly Dictionary<ulong, ulong> ObjectIds = new();
        public readonly Dictionary<string, PlanetMigrationState> WaitingPlayers = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (Func<string> Capture, Action<string> Restore)> _local = new();
        private readonly Dictionary<string, string> _savedLocal = new();
        private long _revision;
        private ulong _nextEntity = 1;
        private readonly Dictionary<ulong, ulong> _networkToStable = new();
        private readonly Dictionary<object, (ulong Network, ulong Stable, int Version)> _tracked = new();
        public byte[] LatestSnapshot => _latest;
        private byte[] _latest;

        public void StartMatch(string matchId)
        {
            Clear();
            MatchId = matchId;
        }

        public void BeginRecovery()
        {
            if (IsRecovering)
            {
                return;
            }

            foreach (var pair in _local)
            {
                _savedLocal[pair.Key] = pair.Value.Capture();
            }
            IsRecovering = true;
            RecoveryStarted?.Invoke();
        }

        public void RegisterLocal(string key, Func<string> capture, Action<string> restore)
        {
            _local[key] = (capture, restore);
            if (_savedLocal.TryGetValue(key, out string value))
            {
                restore(value);
            }
        }

        public void UnregisterLocal(string key)
        {
            if (IsRecovering && _local.TryGetValue(key, out var local))
            {
                _savedLocal[key] = local.Capture();
            }
            _local.Remove(key);
        }

        public byte[] Capture()
        {
            if (CaptureWorld != null)
            {
                var snapshot = CaptureWorld();
                if (snapshot != null)
                {
                    snapshot.MatchId = MatchId;
                    snapshot.Revision = ++_revision;
                    var active = new HashSet<string>(snapshot.Players.Select(p => p.Identity));
                    snapshot.Players = snapshot.Players
                        .Concat(WaitingPlayers.Values.Where(p => !active.Contains(p.Identity))).ToArray();
                    _latest = MatchSnapshotCodec.Encode(snapshot);
                }
            }
            return _latest ?? Array.Empty<byte>();
        }

        public void Accept(byte[] bytes)
        {
            var snapshot = MatchSnapshotCodec.Decode(bytes);
            if (snapshot.MatchId != MatchId)
            {
                throw new InvalidDataException("Migration belongs to another match.");
            }
            if (Pending != null && snapshot.Revision < Pending.Revision)
            {
                throw new InvalidDataException("Stale migration snapshot.");
            }

            BeginRecovery();
            Pending = snapshot;
            RestoredFood = snapshot.Food;
            _revision = snapshot.Revision;
            _latest = bytes;
            ObjectIds.Clear();
            _networkToStable.Clear();
            _tracked.Clear();
            WaitingPlayers.Clear();
            _nextEntity = snapshot.Players.Select(p => p.ObjectId).Concat(snapshot.Bots.Select(p => p.ObjectId))
                    .Concat(snapshot.Food.Select(p => p.ObjectId)).Concat(snapshot.Comets.Select(p => p.ObjectId))
                    .DefaultIfEmpty(0UL).Max() +
                1;
            foreach (var player in snapshot.Players)
            {
                WaitingPlayers.Add(player.Identity, player);
            }
        }

        public PlanetMigrationState TakePlayer(string identity)
        {
            if (!WaitingPlayers.Remove(identity, out var player))
            {
                return null;
            }

            return player;
        }

        public ulong StableId(ulong networkId)
        {
            if (networkId == ulong.MaxValue)
            {
                return ulong.MaxValue;
            }
            if (_networkToStable.TryGetValue(networkId, out ulong stable))
            {
                return stable;
            }

            ulong id = _nextEntity++;
            BindObject(id, networkId);
            return id;
        }

        public void TrackObject(ulong networkId, object instance, int version = 0)
        {
            if (_tracked.TryGetValue(instance, out var old) &&
                old.Network == networkId &&
                old.Version == version &&
                ObjectIds.TryGetValue(old.Stable, out ulong current) &&
                current == networkId)
            {
                return;
            }

            BindObject(_nextEntity++, networkId, instance, version);
        }

        public void PruneTracked(IEnumerable<object> live)
        {
            var active = new HashSet<object>(live);
            foreach (var instance in _tracked.Keys.Where(key => !active.Contains(key)).ToArray())
            {
                var old = _tracked[instance];
                _tracked.Remove(instance);
                if (_networkToStable.TryGetValue(old.Network, out ulong current) && current == old.Stable)
                {
                    _networkToStable.Remove(old.Network);
                    ObjectIds.Remove(old.Stable);
                }
            }
        }

        public void BindObject(ulong stable, ulong networkId, object instance = null, int version = 0)
        {
            if (_networkToStable.TryGetValue(networkId, out ulong previous) && previous != stable)
            {
                ObjectIds.Remove(previous);
            }
            ObjectIds[stable] = networkId;
            _networkToStable[networkId] = stable;
            if (instance != null)
            {
                _tracked[instance] = (networkId, stable, version);
            }
            if (stable >= _nextEntity)
            {
                _nextEntity = stable + 1;
            }
        }

        public void WorldRestored() => Pending = null;

        public void CompleteRecovery()
        {
            IsRecovering = false;
            Failure = null;
            RecoveryCompleted?.Invoke();
        }

        public void Fail(string reason)
        {
            Failure = reason;
            BeginRecovery();
        }

        public void Clear()
        {
            RestoredFood = Array.Empty<FoodMigrationState>();
            MatchId = null;
            Pending = null;
            Failure = null;
            IsRecovering = false;
            Clients.Clear();
            ObjectIds.Clear();
            _networkToStable.Clear();
            _tracked.Clear();
            WaitingPlayers.Clear();
            _local.Clear();
            _savedLocal.Clear();
            _latest = null;
            _revision = 0;
            _nextEntity = 1;
            CaptureWorld = null;
            PausePeers = null;
        }
    }
}
