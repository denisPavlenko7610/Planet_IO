using System;
using System.Collections.Generic;

namespace PlanetIO
{
    public interface IProgressionService
    {
        event Action Changed;

        int Xp { get; }
        int Rank { get; }
        float RankProgress { get; }
        int Dust { get; }
        int StreakDays { get; }
        int LastStreakReward { get; }
        bool CanReroll { get; }
        bool HasClaimableMission { get; }
        IReadOnlyList<MissionView> Missions { get; }

        void RefreshDay();
        RunReward ApplyRun(RunSummary summary);
        bool TryClaimMission(int index);
        bool TryRerollMission(int index);
        bool TrySpendDust(int amount);
        void AddDust(int amount);
    }
}
