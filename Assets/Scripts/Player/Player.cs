using UnityEngine;
using Zenject;
using Dythervin.AutoAttach;


namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        private const float _measurementError = 0.01f;
        
        [Header("Borders")] 
        [SerializeField, Attach(Attach.Scene)] private BordersTrigger _bordersTrigger;

        [Header("player script")] 
        [SerializeField, Attach] private PlayerScale _playerScale;
        [SerializeField, Attach] private InputPlayerSystem _inputPlayerSystem;
        [SerializeField, Attach] private PlayerMovement _playerMovement;

        [Header("Spawner")] 
        [SerializeField] private Transform _pointSpawnTransform;
        private CometsSpawnerLogics _cometsSpawnerLogics;
        private PointsSpawnerLogics _pointsSpawnerLogics;


        [Inject]
        private void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnEnable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler += _playerScale.DecreasePlayerCapacity;
            _inputPlayerSystem.Input += Move;
        }

        private void OnDisable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler -= _playerScale.DecreasePlayerCapacity;
            _inputPlayerSystem.Input -= Move;
        } 
        
        public void SpeedLogics()
        {
            if (_playerScale.CapacityPlayer > _playerScale.MinCapacityPlayer + _measurementError)
            {
                _playerScale.SetPlayerCapacity(-_measurementError);
                _pointsSpawnerLogics.CreatePoint(_pointSpawnTransform);
            }
        }

        private void Move(Vector2 MoveInput) => _playerMovement.Direction = MoveInput;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                _playerScale.SetPlayerCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _playerScale.SetPlayerCapacity(-comet.Capacity);
                _cometsSpawnerLogics.CreateComet(comet);
            }
        }

    }
}