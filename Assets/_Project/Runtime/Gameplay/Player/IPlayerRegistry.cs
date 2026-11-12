using UnityEngine;
using System.Collections.Generic;

namespace PlanetIO
{
    public interface IPlayerRegistry
    {
        int Count { get; }
        Player LocalPlayer { get; }
        void Register(Player player);
        void Unregister(Player player);
        void SetLocal(Player player);
        bool IsAnyPlayerWithinDistance(Vector2 position, float maxDistance);
    }
}
