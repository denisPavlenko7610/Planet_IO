using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public static class PlayerRegistry
    {
        private static readonly HashSet<Player> Players = new();

        public static int Count => Players.Count;

        public static void Register(Player player)
        {
            Players.Add(player);
        }

        public static void Unregister(Player player)
        {
            Players.Remove(player);
        }

        public static bool IsAnyPlayerWithinDistance(Vector2 position, float maxDistance)
        {
            if (Players.Count == 0)
            {
                return false;
            }

            float sqrMax = maxDistance * maxDistance;

            foreach (Player player in Players)
            {
                if (player == null)
                {
                    continue;
                }

                float sqrDist = ((Vector2)player.transform.position - position).sqrMagnitude;
                if (sqrDist <= sqrMax)
                {
                    return true;
                }
            }

            return false;
        }

        public static float GetClosestPlayerDistance(Vector2 position)
        {
            if (Players.Count == 0)
            {
                return float.MaxValue;
            }

            float closest = float.MaxValue;

            foreach (Player player in Players)
            {
                if (player == null)
                {
                    continue;
                }

                float dist = Vector2.Distance((Vector2)player.transform.position, position);
                if (dist < closest)
                {
                    closest = dist;
                }
            }

            return closest;
        }
    }
}
