using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public static class RespawnRules
    {
        public const int CandidateCount = 8;
        public const float FoodSearchRadius = 12f;
        public const int FoodCap = 40;
        public const float SafeDistance = 22f;
        private const float SafetyWeight = 2f;

        public static float Score(int foodCount, float nearestThreatEdgeDistance)
        {
            float food = Mathf.Clamp01(Mathf.Max(0, foodCount) / (float)FoodCap);
            float safety = Mathf.Clamp01(nearestThreatEdgeDistance / SafeDistance);
            return food + SafetyWeight * safety;
        }

        public static int PickBest(IReadOnlyList<float> scores)
        {
            int best = -1;
            float bestScore = float.NegativeInfinity;
            for (int index = 0; index < scores.Count; index++)
            {
                if (scores[index] > bestScore)
                {
                    bestScore = scores[index];
                    best = index;
                }
            }

            return best;
        }
    }
}
