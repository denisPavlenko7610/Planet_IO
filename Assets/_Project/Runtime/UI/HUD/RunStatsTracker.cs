using System;
using UnityEngine;
using VContainer.Unity;

namespace PlanetIO.UI.Hud
{
    public sealed partial class RunStatsTracker : IRunStats, IStartable, ITickable, IDisposable
    {
        private readonly ILocalPlayerProvider _localPlayerProvider;
        private readonly IAnalyticsService _analytics;
        private Player _player;
        private long _runId;
        private int _runIndex;
        private float _duration;
        private float _peakCapacity;
        private int _peakTier = TierRules.MinTier;
        private int _lastTier = TierRules.MinTier;
        private int _kills;
        private int _perksPicked;
        private ushort _lastPerkMask;

        public RunStatsTracker(ILocalPlayerProvider localPlayerProvider, IAnalyticsService analytics)
        {
            _localPlayerProvider = localPlayerProvider ?? throw new ArgumentNullException(nameof(localPlayerProvider));
            _analytics = analytics ?? throw new ArgumentNullException(nameof(analytics));
        }

        public RunSummary Current => new(
            _runId,
            _duration,
            Constants.CapacityToScore(_peakCapacity),
            _peakTier,
            _kills,
            _perksPicked);

        public string KillerName { get; private set; }

        public int KillerTier
        {
            get
            {
                if (string.IsNullOrEmpty(KillerName))
                {
                    return 0;
                }

                var planets = PlanetDirectory.All;
                for (int index = 0; index < planets.Count; index++)
                {
                    PlanetScale planet = planets[index];
                    if (planet != null && planet.IsSpawned && planet.DisplayName == KillerName)
                    {
                        return planet.Tier;
                    }
                }

                return 0;
            }
        }

        public void Start()
        {
            RegisterMigrationRun();
            Player.DefeatAnnounced += OnDefeatAnnounced;
            _localPlayerProvider.LocalPlayerChanged += BindPlayer;
            BindPlayer(_localPlayerProvider.LocalPlayer);
        }

        public void Dispose()
        {
            _migration?.UnregisterLocal("run");
            Player.DefeatAnnounced -= OnDefeatAnnounced;
            _localPlayerProvider.LocalPlayerChanged -= BindPlayer;
            Unbind();
        }

        public void Tick()
        {
            if (_migration?.IsRecovering != true && _player != null && _player.IsSpawned && !_player.IsDefeated)
            {
				_duration += Time.deltaTime;
            }
        }

        public void BeginRun()
        {
            _runId = Math.Max(_runId + 1, DateTime.UtcNow.Ticks);
            _runIndex++;
            _duration = 0f;
            _peakCapacity = _player != null ? _player.Capacity : 0f;
            _peakTier = _player != null ? Mathf.Max(TierRules.MinTier, _player.Tier) : TierRules.MinTier;
            _lastTier = _peakTier;
            _kills = 0;
            _perksPicked = 0;
            _lastPerkMask = 0;
            KillerName = null;
            _analytics.Track(AnalyticsEvents.RunStart, ("run_index", _runIndex));
        }

        private void BindPlayer(Player player)
        {
            Unbind();
            _player = player;
            if (_player == null)
            {
                return;
            }

            _player.Killed += OnKilled;
            _player.CapacityChanged += OnCapacityChanged;
            _player.TierChanged += OnTierChanged;
            _player.PerksChanged += OnPerksChanged;
            _player.PerkOfferShown += OnPerkOfferShown;
            _player.Revived += OnRevived;
            if (_resumeMigrationRun && _player.WasMigrationRestored)
            {
                _resumeMigrationRun = false;
                _lastPerkMask = _player.PerkMask;
            }
            else
			{
				BeginRun();
			}
		}

        private void Unbind()
        {
            if (_player == null)
            {
                return;
            }

            _player.Killed -= OnKilled;
            _player.CapacityChanged -= OnCapacityChanged;
            _player.TierChanged -= OnTierChanged;
            _player.PerksChanged -= OnPerksChanged;
            _player.PerkOfferShown -= OnPerkOfferShown;
            _player.Revived -= OnRevived;
            _player = null;
        }

        private void OnKilled(string victimName, int score)
        {
            _kills++;
        }

        private void OnCapacityChanged(float capacity)
        {
            if (_player != null && !_player.IsDefeated)
            {
                _peakCapacity = Mathf.Max(_peakCapacity, capacity);
            }
        }

        private void OnTierChanged(int tier)
        {
            if (tier > _lastTier)
            {
                _analytics.Track(AnalyticsEvents.TierUp, ("tier", tier), ("time_in_run_s", Mathf.RoundToInt(_duration)));
            }

            _lastTier = tier;
            _peakTier = Mathf.Max(_peakTier, tier);
        }

        private void OnPerksChanged(ushort mask)
        {
            ushort added = (ushort)(mask & ~_lastPerkMask);
            for (int id = 0; id < PerkRules.Count; id++)
            {
                if ((added & (1 << id)) != 0)
                {
                    _perksPicked++;
                    _analytics.Track(AnalyticsEvents.PerkPicked, ("perk_id", id), ("tier", _lastTier));
                }
            }

            _lastPerkMask = mask;
        }

        private void OnPerkOfferShown(PerkId[] perks, float seconds)
        {
            _analytics.Track(
                AnalyticsEvents.PerkOffered,
                ("tier", _lastTier),
                ("perk_ids", string.Join(",", Array.ConvertAll(perks, perk => (int)perk))));
        }

        private void OnRevived()
        {
            KillerName = null;
        }

        private void OnDefeatAnnounced(Player.DefeatAnnouncement announcement)
        {
            if (_player != null && announcement.VictimClientId == _player.OwnerClientId)
            {
                KillerName = announcement.KillerName;
            }
        }
    }
}
