using UnityEngine;
using Zenject;
using Dythervin.AutoAttach;


namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        [Header("Borders")] 
        [SerializeField, Attach(Attach.Scene)] private BordersTrigger _bordersTrigger;

        [Header("player script")] 
        [SerializeField,Attach] private PlayerScale _playerScale;

        [Header("Spawner")] 
        [SerializeField] private Transform _pointSpawnTransform;
        private CometsSpawnerLogic _cometsSpawnerLogic;
        private LogicsPointsSpawner _logicsPointsSpawner;

        private const float _measurementError = 0.01f;

        [Inject]
        private void Construct(CometsSpawnerLogic cometsSpawnerLogic, LogicsPointsSpawner logicsPointsSpawner)
        {
            _cometsSpawnerLogic = cometsSpawnerLogic;
            _logicsPointsSpawner = logicsPointsSpawner;
        }

        private void OnEnable() => _bordersTrigger.OnPlayerTriggeredHandler += _playerScale.DecreasePlayerCapacity;

        private void OnDisable() => _bordersTrigger.OnPlayerTriggeredHandler -= _playerScale.DecreasePlayerCapacity;
        
        public void SpeedLogics()
        {
            if (_playerScale.CapacityPlayer > _playerScale.MinCapacityPlayer + _measurementError)
            {
                _playerScale.SetPlayerCapacity(-_measurementError);
                _logicsPointsSpawner.CreatePoint(_pointSpawnTransform);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                _playerScale.SetPlayerCapacity(point.Capacity);
                _logicsPointsSpawner.CreatePoint(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _playerScale.SetPlayerCapacity(-comet.Capacity);
                _cometsSpawnerLogic.CreateComet(comet);
            }
        }

    }
}