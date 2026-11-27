using UnityEngine;
using System.Collections.Generic;

namespace PlanetIO
{
    public class PlayerRegistryService : IPlayerRegistry
    {
        readonly HashSet<Player> _players = new();

        public int Count => _players.Count;

        public Player LocalPlayer { get; private set; }

        public void Register(Player player)
        {
            _players.Add(player);
        }

        public void Unregister(Player player)
        {
            _players.Remove(player);
            if (LocalPlayer == player)
            {
                LocalPlayer = null;
            }
        }

        public void SetLocal(Player player)
        {
            LocalPlayer = player;
        }

        public bool IsAnyPlayerWithinDistance(Vector2 position, float maxDistance)
        {
            if (_players.Count == 0)
            {
                return false;
            }

            float sqrMax = maxDistance * maxDistance;

            foreach (Player player in _players)
            {
                if (player == null ||
                    !player.IsSpawned ||
                    player.IsDefeated)
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
    }
}
