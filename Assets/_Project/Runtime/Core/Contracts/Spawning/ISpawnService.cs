using UnityEngine;

namespace PlanetIO
{
    public interface ISpawnService<T>
    {
        void SpawnAt(Transform position);
    }
}
