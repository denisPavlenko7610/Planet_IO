using UnityEngine;

namespace Spawner
{
    public interface ISpawner<T> : IInit<T>, IGenerateObjects, ICreateObject, ITransformPosition<T>, IRandomPosition
        where T : MonoBehaviour
    {
        public Vector2 SpawnPositionX { get; set; }
        public Vector2 SpawnPositionY { get; set; }
    }
}