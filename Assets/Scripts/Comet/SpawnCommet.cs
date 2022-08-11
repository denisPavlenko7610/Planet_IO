using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnCommet : CometPool
{
    [SerializeField] private float _secondBetWeenSpawn;
    [SerializeField] private GameObject _prefabComet;
    [SerializeField] private Vector2 _spawnPositionX = new(-223f, 223f);
    [SerializeField] private Vector2 _spawnPositionY = new(-139f, 161.9f);

    private float _elepsedTime = 0;

    private void Awake()
    {
        Initialize(_prefabComet);
    }

    private void Update()
    {
        _elepsedTime += Time.deltaTime;
        
        if( _elepsedTime >= _secondBetWeenSpawn)
        {
            if(TryGetObject(out GameObject cometPrefab))
            {
                _elepsedTime = 0;
                var randomPosition = GenerateRandomPosition();
                var cometTransform = cometPrefab.transform;
                cometTransform.position = randomPosition;
                SetComet(cometPrefab);
            }    
        }
    }


    private void SetComet(GameObject cometPrefab)
    {
        cometPrefab.SetActive(true);
    }
    private Vector2 GenerateRandomPosition() =>
        new (Random.Range(_spawnPositionX.x, _spawnPositionX.y),
             Random.Range(_spawnPositionY.x, _spawnPositionY.y));
}
