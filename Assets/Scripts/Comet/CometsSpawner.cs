using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class CometsSpawner : MonoBehaviour
    {
        [SerializeField] private Vector2 _spawnPositionX = new(-223f, 223f);
        [SerializeField] private Vector2 _spawnPositionY = new(-139f, 161.9f);
        [SerializeField] private float _minCometScale = 0.4f;
        [SerializeField] private float _maxCometScale = 1f;
        
        private CometsPool _cometsPool;

        [Inject]
        private void Construct(CometsPool cometsPool)
        {
            _cometsPool = cometsPool;
        }

        private void Start() => GenerateComet();

        private void GenerateComet()
        {
            for (int i = 0; i < _cometsPool.Count; i++)
            {
                CreateComet();
            }
        }

        public void CreateComet()
        {
            var comet = _cometsPool.Pool?.Get();
            var randomScale = Random.Range(_minCometScale, _maxCometScale);
            if (comet != null)
            {
                SetCometTransform(comet, randomScale);
            }
           
        }

        private void SetCometTransform(Comet comet, float randomScale)
        {
            if (comet == null)
                return;

            var randomPosition = GenerateRandomPosition();
            var cometTransform = comet.transform;
            cometTransform.position = randomPosition;
            var zPosition = 1;
            cometTransform.localScale = new Vector3(randomScale, randomScale, zPosition);
        }

        private Vector2 GenerateRandomPosition() =>
            new(Random.Range(_spawnPositionX.x, _spawnPositionX.y),
                Random.Range(_spawnPositionY.x, _spawnPositionY.y));
    }
}