using System;
using UnityEngine;

namespace PlanetIO
{
    [Serializable]
    public sealed class MissionEntry
    {
        public int Definition;
        public int Progress;
        public bool Claimed;

        public bool IsComplete => Progress >= MissionRules.Target(Definition);
    }

    [Serializable]
    public sealed class ProgressionState
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public int Xp;
        public int Dust;
        public int StreakDays;
        public int LastLoginDay;
        public int MissionDay;
        public bool RerollUsed;
        public MissionEntry[] Missions = Array.Empty<MissionEntry>();
        public long LedgerRunId = -1;
        public int LedgerXp;
        public int LedgerKills;
        public int LedgerPerks;

        public int Rank => XpRules.RankForXp(Xp);

        public float RankProgress => XpRules.Progress(Xp);

        public bool IsValid()
        {
            if (Version != CurrentVersion || Xp < 0 || Dust < 0 || StreakDays < 0 || Missions == null)
            {
                return false;
            }

            foreach (MissionEntry mission in Missions)
            {
                if (mission == null || !MissionRules.IsValid(mission.Definition) || mission.Progress < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public int RefreshDay(int dayNumber)
        {
            if (dayNumber < LastLoginDay)
            {
                return 0;
            }

            int reward = 0;
            if (dayNumber != LastLoginDay)
            {
                StreakDays = LastLoginDay > 0 && dayNumber == LastLoginDay + 1 ? StreakDays + 1 : 1;
                reward = DustRules.StreakReward(StreakDays);
                Dust += reward;
                LastLoginDay = dayNumber;
            }

            if (MissionDay != dayNumber || Missions == null || Missions.Length != MissionRules.DailyCount)
            {
                var definitions = new int[MissionRules.DailyCount];
                MissionRules.PickDaily(dayNumber, definitions);
                Missions = new MissionEntry[MissionRules.DailyCount];
                for (int index = 0; index < Missions.Length; index++)
                {
                    Missions[index] = new MissionEntry { Definition = definitions[index] };
                }

                MissionDay = dayNumber;
                RerollUsed = false;
            }

            return reward;
        }

        public RunReward ApplyRun(RunSummary summary)
        {
            bool newRun = summary.RunId != LedgerRunId;
            if (newRun)
            {
                LedgerRunId = summary.RunId;
                LedgerXp = 0;
                LedgerKills = 0;
                LedgerPerks = 0;
            }

            int runXp = XpRules.RunXp(summary);
            int xpGain = Mathf.Max(0, runXp - LedgerXp);
            int xpBefore = Xp;
            int rankBefore = Rank;

            Xp += xpGain;
            int dustGain = DustRules.RunDust(xpGain) + DustRules.RankUpDust(rankBefore, Rank);
            Dust += dustGain;
            LedgerXp = Mathf.Max(LedgerXp, runXp);

            int killsDelta = Mathf.Max(0, summary.Kills - LedgerKills);
            int perksDelta = Mathf.Max(0, summary.PerksPicked - LedgerPerks);
            LedgerKills = Mathf.Max(LedgerKills, summary.Kills);
            LedgerPerks = Mathf.Max(LedgerPerks, summary.PerksPicked);

            int completed = 0;
            foreach (MissionEntry mission in Missions)
            {
                if (mission.Claimed)
                {
                    continue;
                }

                MissionMetric metric = MissionRules.Metric(mission.Definition);
                int runValue = metric switch
                {
                    MissionMetric.Kills => killsDelta,
                    MissionMetric.PeakTier => summary.PeakTier,
                    MissionMetric.SurviveSeconds => Mathf.FloorToInt(summary.DurationSeconds),
                    MissionMetric.PeakScore => summary.PeakScore,
                    MissionMetric.Perks => perksDelta,
                    _ => newRun ? 1 : 0
                };

                bool wasComplete = mission.IsComplete;
                mission.Progress = MissionRules.Advance(metric, mission.Progress, runValue);
                if (!wasComplete && mission.IsComplete)
                {
                    completed++;
                }
            }

            return new RunReward(xpGain, dustGain, xpBefore, Xp, rankBefore, Rank, completed);
        }

        public bool TryClaim(int index)
        {
            if (index < 0 || index >= Missions.Length)
            {
                return false;
            }

            MissionEntry mission = Missions[index];
            if (mission.Claimed || !mission.IsComplete)
            {
                return false;
            }

            mission.Claimed = true;
            Dust += MissionRules.Reward(mission.Definition);
            return true;
        }

        public bool TryReroll(int index)
        {
            if (RerollUsed || index < 0 || index >= Missions.Length)
            {
                return false;
            }

            MissionEntry mission = Missions[index];
            if (mission.Claimed || mission.Progress > 0)
            {
                return false;
            }

            var inUse = new int[Missions.Length];
            for (int slot = 0; slot < Missions.Length; slot++)
            {
                inUse[slot] = Missions[slot].Definition;
            }

            int replacement = MissionRules.PickReplacement(MissionDay, index, inUse);
            if (replacement == mission.Definition)
            {
                return false;
            }

            mission.Definition = replacement;
            RerollUsed = true;
            return true;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Dust < amount)
            {
                return false;
            }

            Dust -= amount;
            return true;
        }

        public void AddDust(int amount)
        {
            Dust += Mathf.Max(0, amount);
        }

        public bool HasClaimableMission()
        {
            foreach (MissionEntry mission in Missions)
            {
                if (mission.IsComplete && !mission.Claimed)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
