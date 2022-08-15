using System.Diagnostics;
using Pool;
using Spawner;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class PointsSpawner : MonoBehaviour, ISpawner<Point>
    {
        [SerializeField] private float _minPointScale = 0.4f;
        [SerializeField] private float _maxPointScale = 1f;
        [field: SerializeField] public Vector2 SpawnPositionX { get; set; } = new(-223f, 223f);
        [field: SerializeField] public Vector2 SpawnPositionY { get; set; } = new(-139f, 161.9f);

        private PointsPool _pointsPool;

        public void Init(IPool<Point> pool)
        {
            _pointsPool = (PointsPool)pool;
            _pointsPool.Init();
            GenerateObjects();
        }

        public void GenerateObjects()
        {
#if UNITY_EDITOR
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            for (int i = 0; i < _pointsPool.Count; i++)
            {
                CreateObject();
            }

#if UNITY_EDITOR
            stopwatch.Stop();
            print(stopwatch.ElapsedMilliseconds);
#endif
        }

        public void CreateObject()
        {
            var point = _pointsPool.Pool?.Get();
            var randomScale = Random.Range(_minPointScale, _maxPointScale);
            if (point != null)
            {
                point.Capacity = randomScale;
                SetTransform(point, randomScale);
            }
        }

        public Vector2 GetRandomPosition() =>
            new(Random.Range(SpawnPositionX.x, SpawnPositionX.y),
                Random.Range(SpawnPositionY.x, SpawnPositionY.y));

        public void SetTransform(Point @object, float randomScale)
        {
            var randomPosition = GetRandomPosition();
            var pointTransform = @object.transform;
            pointTransform.position = randomPosition;
            var zPosition = 1;
            pointTransform.localScale = new Vector3(randomScale, randomScale, zPosition);
        }
    }
}