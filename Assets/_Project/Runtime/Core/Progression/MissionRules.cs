using System;
using UnityEngine;

namespace PlanetIO
{
    public enum MissionMetric : byte
    {
        Kills,
        PeakTier,
        SurviveSeconds,
        PeakScore,
        Perks,
        Runs
    }

    public static class MissionRules
    {
        public const int DailyCount = 3;
        public const int DefinitionCount = 7;

        public static MissionMetric Metric(int definition) => definition switch
        {
            0 => MissionMetric.Kills,
            1 => MissionMetric.PeakTier,
            2 => MissionMetric.SurviveSeconds,
            3 => MissionMetric.PeakScore,
            4 => MissionMetric.Perks,
            5 => MissionMetric.Runs,
            _ => MissionMetric.Kills
        };

        public static int Target(int definition) => definition switch
        {
            0 => 3,
            1 => 3,
            2 => 240,
            3 => 90,
            4 => 3,
            5 => 3,
            _ => 8
        };

        public static int Reward(int definition) => definition switch
        {
            0 => 40,
            1 => 50,
            2 => 50,
            3 => 40,
            4 => 40,
            5 => 30,
            _ => 80
        };

        public static bool IsValid(int definition) => definition >= 0 && definition < DefinitionCount;

        public static bool IsCumulative(MissionMetric metric) =>
            metric == MissionMetric.Kills || metric == MissionMetric.Perks || metric == MissionMetric.Runs;

        public static int Advance(MissionMetric metric, int current, int runValue)
        {
            int value = Mathf.Max(0, runValue);
            return IsCumulative(metric) ? current + value : Mathf.Max(current, value);
        }

        public static void PickDaily(int dayNumber, int[] result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var pool = new int[DefinitionCount];
            for (int index = 0; index < pool.Length; index++)
            {
                pool[index] = index;
            }

            var random = new System.Random(dayNumber * 7919 + 13);
            for (int index = pool.Length - 1; index > 0; index--)
            {
                int swap = random.Next(index + 1);
                (pool[index], pool[swap]) = (pool[swap], pool[index]);
            }

            int count = Mathf.Min(result.Length, pool.Length);
            for (int index = 0; index < count; index++)
            {
                result[index] = pool[index];
            }
        }

        public static int PickReplacement(int dayNumber, int slot, int[] inUse)
        {
            var random = new System.Random(dayNumber * 104729 + slot * 31 + 7);
            int start = random.Next(DefinitionCount);
            for (int offset = 0; offset < DefinitionCount; offset++)
            {
                int candidate = (start + offset) % DefinitionCount;
                if (Array.IndexOf(inUse, candidate) < 0)
                {
                    return candidate;
                }
            }

            return inUse[slot];
        }
    }
}
