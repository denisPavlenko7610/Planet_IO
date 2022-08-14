using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class PointsSpawner : MonoBehaviour
    {
        [SerializeField] private Vector2 _spawnPositionX = new(-223f, 223f);
        [SerializeField] private Vector2 _spawnPositionY = new(-139f, 161.9f);
        [SerializeField] private float _minPointScale = 0.4f;
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
            var point = _pointsPool.Pool?.Get();
            var randomScale = Random.Range(_minPointScale, _maxPointScale);
            if (point != null)
            {
                point.Capacity = randomScale;
                SetPointTransform(point, randomScale);
            }
        }

        private void GeneratePoints()
        {
            for (int i = 0; i < _pointsPool.Count; i++)
            {
                CreatePoint();
            }
        }

        private void SetPointTransform(Point point, float randomScale)
        {
            var randomPosition = GenerateRandomPosition();
            var pointTransform = point.transform;
            pointTransform.position = randomPosition;
            var zPosition = 1;
            pointTransform.localScale = new Vector3(randomScale, randomScale, zPosition);
        }

        private Vector2 GenerateRandomPosition() =>
            new(Random.Range(_spawnPositionX.x, _spawnPositionX.y),
                Random.Range(_spawnPositionY.x, _spawnPositionY.y));
    }
}