using UnityEngine;

namespace PlanetIO
{
    public sealed partial class Player
    {
        private readonly float[] _respawnScores = new float[RespawnRules.CandidateCount];
        private readonly Vector2[] _respawnCandidates = new Vector2[RespawnRules.CandidateCount];

        private Vector2 PickRespawnPosition()
        {
            for (int index = 0; index < RespawnRules.CandidateCount; index++)
            {
				Vector2 candidate = WorldBounds.RandomPosition();
                _respawnCandidates[index] = candidate;
                _respawnScores[index] = RespawnRules.Score(CountFoodAround(candidate), NearestThreatEdgeDistance(candidate));
            }

            int best = RespawnRules.PickBest(_respawnScores);
			return best >= 0 ? _respawnCandidates[best] : WorldBounds.RandomPosition();
        }

        private int CountFoodAround(Vector2 position)
        {
            FoodRespawnService.Query(position, RespawnRules.FoodSearchRadius, _foodBuffer);
			return _foodBuffer.Count;
        }

        private float NearestThreatEdgeDistance(Vector2 position)
        {
            float nearest = float.PositiveInfinity;
            var planets = PlanetDirectory.All;
            for (int index = 0; index < planets.Count; index++)
            {
				PlanetBody planet = planets[index];
                if (planet == null || planet == this || !planet.IsSpawned || !planet.CanDevour(this))
                {
                    continue;
                }

                if (planet is Player { IsDefeated: true })
                {
                    continue;
                }

                float edgeDistance = Vector2.Distance(position, planet.transform.position) - planet.BodyRadius;
                nearest = Mathf.Min(nearest, edgeDistance);
            }

            return nearest;
        }
    }
}
