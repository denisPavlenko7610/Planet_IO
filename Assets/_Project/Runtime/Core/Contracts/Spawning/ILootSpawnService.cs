using UnityEngine;

namespace PlanetIO
{
    public interface ILootSpawnService
    {
        ILootDrop SpawnLoot(Vector2 position, float sourceCapacity);
    }
}
