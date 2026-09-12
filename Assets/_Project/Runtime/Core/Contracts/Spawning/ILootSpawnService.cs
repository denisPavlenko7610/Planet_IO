using UnityEngine;

namespace PlanetIO
{
    public interface ILootSpawnService
    {
        void SpawnLoot(Vector2 position, float sourceCapacity);
    }
}
