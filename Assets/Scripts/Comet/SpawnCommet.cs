using UnityEngine;
using Random = UnityEngine.Random;
 
namespace PlanetIO
{
    public class SpawnCommet : MonoBehaviour
    {
        [SerializeField] private float _secondBetWeenSpawn;
        [SerializeField] private Vector2 _spawnPositionX = new(-223f, 223f);
        [SerializeField] private Vector2 _spawnPositionY = new(-139f, 161.9f);
        [SerializeField] private CometPool _cometPool;

        private float _elepsedTime = 0;


        private void Start()
        {
            GenerateComet();
        }

        private void Update()
        {
            _elepsedTime += Time.deltaTime;
            if(_elepsedTime >= _secondBetWeenSpawn)
            {
                _elepsedTime = 0;
                CreateComet();
            }
        }

        private void GenerateComet()
        {
            for(int i = 0; i < _cometPool.CometCount; i++)
            {
                CreateComet();
            }
        }
        public void CreateComet()
        {
            var comet = _cometPool.PoolComet.Get();
            SetCometTransform(comet);
        }

        private void SetCometTransform(Comet comet)
        {
            var randomPosition = GenerateRandomPosition();
            var cometTransform = comet.transform;
            cometTransform.position = randomPosition;
        }
        private Vector2 GenerateRandomPosition() =>
            new(Random.Range(_spawnPositionX.x, _spawnPositionX.y),
                 Random.Range(_spawnPositionY.x, _spawnPositionY.y));
    }
}

