using Pool;
using Spawner;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class CometsSpawner : MonoBehaviour, ISpawner<Comet>
    {
        [SerializeField] private float _minCometScale = 0.4f;
        [SerializeField] private float _maxCometScale = 1f;
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
            var randomScale = Random.Range(_minCometScale, _maxCometScale);
            if (comet != null)
            {
                comet.Capacity = randomScale;
                SetTransform(comet, randomScale);
            }
        }

        public void SetTransform(Comet @object, float randomScale)
        {
            if (@object == null)
                return;

            var randomPosition = GetRandomPosition();
            var cometTransform = @object.transform;
            cometTransform.position = randomPosition;
            var zPosition = 1;
            cometTransform.localScale = new Vector3(randomScale, randomScale, zPosition);
        }

        public Vector2 GetRandomPosition() =>
            new(Random.Range(SpawnPositionX.x, SpawnPositionX.y),
                Random.Range(SpawnPositionY.x, SpawnPositionY.y));
    }
}