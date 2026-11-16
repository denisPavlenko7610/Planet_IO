using System;
using UnityEngine;
using VContainer;

namespace PlanetIO.UI.Hud
{
    public sealed partial class DefeatPresenter
    {
        private MatchMigrationStore _migration;
        private bool _restoredReward;

        [Serializable] private sealed class SavedDefeat
        {
            public bool ContinueUsed, DoubleDust, Defeated;
            public float Peak;
            public int Score;
            public int Xp, Dust, XpBefore, XpAfter, RankBefore, RankAfter, Missions;
        }

        [Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;

        private void RegisterMigrationDefeat()
        {
            _migration?.RegisterLocal(
                "defeat",
                () => JsonUtility.ToJson(
                    new SavedDefeat
                    {
                        ContinueUsed = _continueUsedThisLife,
                        DoubleDust = _doubleDustClaimed, Peak = _peakCapacity, Score = _finalScore,
                        Defeated = _isDefeated,
                        Xp = _lastReward.XpGained, Dust = _lastReward.DustGained, XpBefore = _lastReward.XpBefore,
                        XpAfter = _lastReward.XpAfter,
                        RankBefore = _lastReward.RankBefore, RankAfter = _lastReward.RankAfter,
                        Missions = _lastReward.MissionsCompleted
                    }
                ),
                json =>
                {
                    var s = JsonUtility.FromJson<SavedDefeat>(json);
                    _continueUsedThisLife = s.ContinueUsed;
                    _doubleDustClaimed = s.DoubleDust;
                    _peakCapacity = s.Peak;
                    _finalScore = s.Score;
                    _restoredReward = s.Defeated;
                    _lastReward = new RunReward(
                        s.Xp,
                        s.Dust,
                        s.XpBefore,
                        s.XpAfter,
                        s.RankBefore,
                        s.RankAfter,
                        s.Missions
                    );
                }
            );
        }
    }
}
