using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class PointsPool : MonoBehaviour
    {
        [SerializeField] private Vector2 _spawnPositionX = new(-223f, 223f);
        [SerializeField] private Vector2 _spawnPositionY = new(-139f, 161.9f);
        [field: SerializeField] public int PointCount { get; private set; } = 1000;
        [field:SerializeField] public Point[] Points { get; private set; }
        [field:SerializeField] public IObjectPool<Point> Pool { get; private set; }
        private void Awake()
        {
            Pool = new ObjectPool<Point>(OnCreatePoint, OnGetPoint, OnReleasePoint, OnDestroyPoint, true,
                PointCount, PointCount);
        }

        private void Start()
        {
            GeneratePoints();
        }

        private void GeneratePoints()
        {
            for (int i = 0; i < PointCount; i++)
            {
                var point = Pool.Get();
                var randomPosition = GenerateRandomPosition();
                point.transform.position = randomPosition;
                point.transform.parent = transform;
            }
        }
        
        private void OnDestroyPoint(Point point)
        {
        }

        private void OnGetPoint(Point point)
        {
        }

        private void OnReleasePoint(Point point)
        {
            Pool.Release(point);
            Destroy(point);
        }

        private Point OnCreatePoint()
        {
            var randomNumber = Random.Range(0, Points.Length);
            return Instantiate(Points[randomNumber]);
        }

        private Vector2 GenerateRandomPosition() =>
            new(Random.Range(_spawnPositionX.x, _spawnPositionX.y),
                Random.Range(_spawnPositionY.x, _spawnPositionY.y));
    }
}