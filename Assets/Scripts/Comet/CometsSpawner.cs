using Pool;
using Spawner;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class CometsSpawner : MonoBehaviour, ISpawner<Comet>
    {
        [field: SerializeField] public Vector2 SpawnPositionX { get; set; } = new(-223f, 223f);
        [field: SerializeField] public Vector2 SpawnPositionY { get; set; } = new(-139f, 161.9f);

        private CometsPool _cometsPool;

        public void Init(IPool<Comet> pool)
        {
            _cometsPool = (CometsPool)pool;
            _cometsPool.Init();
            GenerateObjects();
        }

        public void GenerateObjects()
        {
            for (int i = 0; i < _cometsPool.Count; i++)
            {
                CreateObject();
            }
        }

        public void CreateObject()
        {
            var comet = _cometsPool.Pool?.Get();
            SetTransform(comet, comet.transform.localScale.x);
        }

        public void SetTransform(Comet @object, float randomScale)
        {
            if (@object == null)
                return;

            var randomPosition = GetRandomPosition();
            var cometTransform = @object.transform;
            cometTransform.position = randomPosition;
        }

        public Vector2 GetRandomPosition() =>
            new(Random.Range(SpawnPositionX.x, SpawnPositionX.y),
                Random.Range(SpawnPositionY.x, SpawnPositionY.y));
    }
}