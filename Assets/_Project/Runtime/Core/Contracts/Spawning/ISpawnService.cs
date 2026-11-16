using UnityEngine;

namespace PlanetIO
{
    public interface ISpawnService<T>
    {
        T SpawnAt(Transform position, float nutrition);
    }
}
