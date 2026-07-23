using UnityEngine;

namespace Planet_IO
{
    public interface ISpawnService<T>
    {
        void SpawnAt(Transform position);
    }
}
