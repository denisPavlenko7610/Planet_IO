using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace PlanetIO.Application
{
    public sealed partial class WorldDirector : IWorldEvents, IStartable, ITickable, IDisposable
    {
        private enum Phase : byte
        {
            Idle,
            Warning,
            Active
        }

        private const float ServerStepSeconds = 0.1f;
        private const int MeteorCometCount = 12;
        private const float MeteorStagger = 6f;
        private const float WormholeHitFactor = 0.3f;
        private const float BlackHoleHitFactor = 0.5f;
        private const float LootMassFraction = 0.6f;

        private readonly NetworkManager _networkManager;
        private readonly ILootSpawnService _loot;
        private readonly IGameStateService _gameState;
        private readonly IAnalyticsService _analytics;
        private readonly List<CometMovement> _launched = new();
        private readonly HashSet<ulong> _blackHoleVictims = new();
        private readonly Dictionary<ulong, double> _portalReady = new();
        private Phase _phase;
        private CosmicEvent _event;
        private int _activeIndex = -1;
        private int _seed;
        private float _serverTimer;
        private float _absorbedMass;
        private float _lootScale = 1f;
        private Vector2 _portalA;
        private Vector2 _portalB;

        public WorldDirector(
            NetworkManager networkManager,
            ILootSpawnService loot,
            IGameStateService gameState,
            IAnalyticsService analytics)
        {
            _networkManager = networkManager ?? throw new ArgumentNullException(nameof(networkManager));
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public event Action<CosmicEventType> Warned;
        public event Action<CosmicEventType> Started;
        public event Action<CosmicEventType> Ended;

        public DailyModifier Modifier { get; private set; }

        public bool HasMarker => _phase != Phase.Idle;

        public CosmicEventType MarkerType => _phase != Phase.Idle ? _event.Type : CosmicEventType.None;

        public Vector2 MarkerPosition => _phase != Phase.Idle ? _event.PositionAt(OrbitClock.Now) : Vector2.zero;

        public void Start()
        {
            int day = (int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;
            _seed = WorldSeedRules.FromDay(day);
            Modifier = DailyModifierRules.ForDay(day);
            _lootScale = DailyModifierRules.LootScale(Modifier);

            MapFeatureField.Reset();
            MapFeatureField.GravityScale = DailyModifierRules.GravityScale(Modifier);
            BuildMap();
        }

        public void Dispose()
        {
            MapFeatureField.Reset();
            DestroyVisuals();
        }

        public void Tick()
        {
            if (_root == null)
            {
                return;
            }

            double now = OrbitClock.Now;
            if (_gameState.IsGameplayActive)
            {
                UpdatePhase(now);
            }

            UpdateAnchor(now);
            UpdateVisuals(now);

            if (!_networkManager.IsServer || !_gameState.IsGameplayActive)
            {
                return;
            }

            _serverTimer += Time.deltaTime;
            if (_serverTimer >= ServerStepSeconds)
            {
                _serverTimer = 0f;
                ServerStep(now);
            }
        }

        private void BuildMap()
        {
            var stars = new Vector2[MapFeatureRules.StarCount];
            MapFeatureRules.Stars(_seed, stars);
            for (int index = 0; index < stars.Length; index++)
            {
                MapFeatureField.SetAnchor(
                    index,
                    new MapFeatureField.Anchor(
                        stars[index],
                        MapFeatureRules.StarRadius,
                        MapFeatureRules.StarZoneRadius,
                        MapFeatureRules.StarStrength));
            }

            var nebulae = new MapDisc[MapFeatureRules.NebulaCount];
            MapFeatureRules.Nebulae(_seed, nebulae);
            MapFeatureField.SetNebulae(nebulae);

            MapFeatureRules.Wormholes(_seed, out _portalA, out _portalB);
            BuildVisuals(stars, nebulae);
        }

        private void UpdatePhase(double now)
        {
            bool found = CosmicEventRules.TryFindActive(_seed, now, out CosmicEvent current);

            if (found && current.Index != _activeIndex)
            {
                if (_phase != Phase.Idle)
                {
                    FinishEvent();
                }

                _activeIndex = current.Index;
                _event = current;
                _phase = Phase.Warning;
                Warned?.Invoke(current.Type);
            }

            if (_phase == Phase.Warning && now >= _event.StartTime)
            {
                _phase = Phase.Active;
                BeginEvent();
            }

            if (!found && _phase != Phase.Idle)
            {
                FinishEvent();
            }
        }

        private void BeginEvent()
        {
            Started?.Invoke(_event.Type);
            _analytics.Track(AnalyticsEvents.EventStart, ("event_id", (int)_event.Type));

            if (!_networkManager.IsServer)
            {
                return;
            }

            _blackHoleVictims.Clear();
            _absorbedMass = 0f;
            if (_event.Type == CosmicEventType.MeteorShower)
            {
                LaunchMeteors();
            }
        }

        private void FinishEvent()
        {
            CosmicEventType type = _event.Type;
            if (_networkManager.IsServer)
            {
                ServerFinish();
            }

            if (type == CosmicEventType.Supernova)
            {
                GameVfx.Death(_event.Position, new Color(1f, 0.85f, 0.3f), 2f);
            }

            _phase = Phase.Idle;
            _activeIndex = -1;
            Ended?.Invoke(type);
        }

        private void UpdateAnchor(double now)
        {
            if (_phase == Phase.Active && _event.Type == CosmicEventType.BlackHole)
            {
                MapFeatureField.SetAnchor(
                    MapFeatureField.BlackHoleSlot,
                    new MapFeatureField.Anchor(
                        _event.PositionAt(now),
                        CosmicEventRules.BlackHoleRadius,
                        CosmicEventRules.BlackHoleZoneRadius,
                        CosmicEventRules.BlackHoleStrength));
                return;
            }

            MapFeatureField.ClearAnchor(MapFeatureField.BlackHoleSlot);
        }

        private void ServerStep(double now)
        {
            if (_phase == Phase.Active && _event.Type == CosmicEventType.BlackHole)
            {
                DamageInsideBlackHole(now);
            }

            UpdateWormholes(now);
        }

        private void DamageInsideBlackHole(double now)
        {
            Vector2 center = _event.PositionAt(now);
            var planets = PlanetDirectory.All;
            for (int index = 0; index < planets.Count; index++)
            {
                PlanetScale planet = planets[index];
                if (!IsAlive(planet) || _blackHoleVictims.Contains(planet.NetworkObjectId))
                {
                    continue;
                }

                float reach = CosmicEventRules.BlackHoleRadius + planet.BodyRadius * BlackHoleHitFactor;
                if (((Vector2)planet.transform.position - center).sqrMagnitude > reach * reach)
                {
                    continue;
                }

                if (planet.TryApplyHazard(CosmicEventRules.BlackHoleMassFraction, out float dealt))
                {
                    _blackHoleVictims.Add(planet.NetworkObjectId);
                    _absorbedMass += dealt;
                }
            }
        }

        private void UpdateWormholes(double now)
        {
            var planets = PlanetDirectory.All;
            for (int index = 0; index < planets.Count; index++)
            {
                PlanetScale planet = planets[index];
                if (!IsAlive(planet))
                {
                    continue;
                }

                ulong id = planet.NetworkObjectId;
                if (_portalReady.TryGetValue(id, out double readyAt) && now < readyAt)
                {
                    continue;
                }

                Vector2 position = planet.transform.position;
                float reach = MapFeatureRules.WormholeRadius + planet.BodyRadius * WormholeHitFactor;
                if ((position - _portalA).sqrMagnitude <= reach * reach)
                {
                    Teleport(planet, _portalA, _portalB, now);
                }
                else if ((position - _portalB).sqrMagnitude <= reach * reach)
                {
                    Teleport(planet, _portalB, _portalA, now);
                }
            }
        }

        private void Teleport(PlanetScale planet, Vector2 entrance, Vector2 exit, double now)
        {
            Vector2 away = (Vector2)planet.transform.position - entrance;
            Vector2 direction = away.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude
                ? away.normalized
                : Vector2.right;
            Vector2 target = exit + direction * (MapFeatureRules.WormholeExitOffset + planet.BodyRadius);
            _portalReady[planet.NetworkObjectId] = now + MapFeatureRules.WormholeCooldownSeconds;
            planet.Teleport(Constants.ClampToWorld(target));
        }

        private void LaunchMeteors()
        {
            Comet[] comets = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude);
            int count = Mathf.Min(MeteorCometCount, comets.Length);
            Vector2 direction = _event.Direction;
            Vector2 perpendicular = new(-direction.y, direction.x);
            float travel = (float)(_event.Speed * _event.DurationSeconds);
            float laneWidth = CosmicEventRules.MeteorBandHalfWidth * 2f / Mathf.Max(1, count);

            _launched.Clear();
            for (int index = 0; index < count; index++)
            {
                if (!comets[index].TryGetComponent(out CometMovement movement))
                {
                    continue;
                }

                float lane = (index - (count - 1) * 0.5f) * laneWidth;
                Vector2 start = _event.Position - direction * (travel * 0.5f)
                                + perpendicular * lane
                                - direction * (index * MeteorStagger);
                movement.Launch(start, direction, _event.Speed + UnityEngine.Random.Range(-1f, 1f));
                _launched.Add(movement);
            }
        }

        private void ServerFinish()
        {
            switch (_event.Type)
            {
                case CosmicEventType.MeteorShower:
                    foreach (CometMovement movement in _launched)
                    {
                        if (movement != null)
                        {
                            movement.Randomize();
                        }
                    }

                    _launched.Clear();
                    DropLootAlongBand();
                    break;

                case CosmicEventType.Supernova:
                    DropSupernovaLoot();
                    break;

                case CosmicEventType.BlackHole:
                    Vector2 exit = _event.PositionAt(_event.EndTime);
                    float mass = Mathf.Max(0.3f, _absorbedMass) * _lootScale;
                    _loot.SpawnLoot(Constants.ClampToWorld(exit), mass / LootMassFraction);
                    break;
            }
        }

        private void DropLootAlongBand()
        {
            Vector2 direction = _event.Direction;
            for (int index = -2; index <= 2; index++)
            {
                Vector2 point = _event.Position + direction * (index * 35f);
                _loot.SpawnLoot(Constants.ClampToWorld(point), 0.8f * _lootScale);
            }
        }

        private void DropSupernovaLoot()
        {
            const int piles = 4;
            for (int index = 0; index < piles; index++)
            {
                float angle = index * (360f / piles) * Mathf.Deg2Rad;
                Vector2 point = _event.Position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f;
                _loot.SpawnLoot(Constants.ClampToWorld(point), 2f * _lootScale);
            }
        }

        private static bool IsAlive(PlanetScale planet)
        {
            return planet != null && planet.isActiveAndEnabled && planet.IsSpawned && planet.AreMoonsVisible;
        }
    }
}
