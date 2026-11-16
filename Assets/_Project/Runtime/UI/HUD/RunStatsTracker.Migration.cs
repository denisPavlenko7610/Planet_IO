using System;
using UnityEngine;
using VContainer;

namespace PlanetIO.UI.Hud
{
    public sealed partial class RunStatsTracker
    {
        private MatchMigrationStore _migration;
        private bool _resumeMigrationRun;

        [Serializable] private sealed class SavedRun
        {
            public long Id;
            public int Index, PeakTier, LastTier, Kills, Perks;
            public float Duration, PeakMass;
            public ushort PerkMask;
            public string Killer;
        }

        [Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;

        private void RegisterMigrationRun()
        {
            _migration?.RegisterLocal(
                "run",
                () => JsonUtility.ToJson(
                    new SavedRun
                    {
                        Id = _runId, Index = _runIndex,
                        PeakTier = _peakTier, LastTier = _lastTier, Kills = _kills, Perks = _perksPicked,
                        Duration = _duration,
                        PeakMass = _peakCapacity, PerkMask = _lastPerkMask, Killer = KillerName
                    }
                ),
                json =>
                {
                    var s = JsonUtility.FromJson<SavedRun>(json);
                    _runId = s.Id;
                    _runIndex = s.Index;
                    _peakTier = s.PeakTier;
                    _lastTier = s.LastTier;
                    _kills = s.Kills;
                    _perksPicked = s.Perks;
                    _duration = s.Duration;
                    _peakCapacity = s.PeakMass;
                    _lastPerkMask = s.PerkMask;
                    KillerName = s.Killer;
                    _resumeMigrationRun = true;
                }
            );
        }
    }
}
