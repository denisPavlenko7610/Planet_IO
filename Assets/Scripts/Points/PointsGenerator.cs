using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class PointsGenerator : MonoBehaviour
    {
        [SerializeField] private Vector2 _spawnPositionX = new(-223f, 223f);
        [SerializeField] private Vector2 _spawnPositionY = new(-139f, 161.9f);
        [SerializeField] private float _minPointScale = 0.05f;
        [SerializeField] private float _maxPointScale = 1f;
        
        private PointsPool _pointsPool;

        [Inject]
        private void Construct(PointsPool pointsPool)
        {
            _pointsPool = pointsPool;
        }
        private void Start()
        {
            GeneratePoints();
        }

        public void CreatePoint()
        {
            var point = _pointsPool.Pool.Get();
            var randomScale = Random.Range(_minPointScale, _maxPointScale);
            point.Capacity = randomScale;
            SetPointTransform(point, randomScale);
        }

        private void GeneratePoints()
        {
            for (int i = 0; i < _pointsPool.PointCount; i++)
            {
                CreatePoint();
            }
        }

        private void SetPointTransform(Point point, float randomScale)
        {
            var randomPosition = GenerateRandomPosition();
            var pointTransform = point.transform;
            pointTransform.position = randomPosition;
            pointTransform.localScale = new Vector3(randomScale, randomScale, 1);
        }

        private Vector2 GenerateRandomPosition() =>
            new(Random.Range(_spawnPositionX.x, _spawnPositionX.y),
                Random.Range(_spawnPositionY.x, _spawnPositionY.y));
    }
}