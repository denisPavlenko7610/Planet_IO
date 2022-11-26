using RDTools.AutoAttach;
using UnityEngine;
using Zenject;

namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : PlayerScale
    {
        [Header("Borders")] 
        [SerializeField, Attach(Attach.Scene)]
        private BordersTrigger _bordersTrigger;
        
        [Header("player script")] 
        [SerializeField, Attach] private InputPlayerSystem _inputPlayerSystem;
        [SerializeField, Attach] private PlayerMovement _playerMovement;
        
        [Header("Spawner")] 
        [SerializeField] private Transform _pointSpawnTransform;
        
        [Header("Spawner")] 
        private CometsSpawnerLogics _cometsSpawnerLogics;
        private PointsSpawnerLogics _pointsSpawnerLogics;
        
        private const float _measurementError = 0.01f;
        
        [Inject]
        public void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnEnable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler += DecreaseCapacity;
            _inputPlayerSystem.Input += Move;
        }

        private void OnDisable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler -= DecreaseCapacity;
            _inputPlayerSystem.Input -= Move;
        }
        public void EnableBoost()
        {
            if (Capacity > MinCapacity + _measurementError)
            {
                DecreasePlayerCapacity();
                CreatePoints();
            }
        }

        private void CreatePoints() => _pointsSpawnerLogics.CreatePoint(_pointSpawnTransform);

        private void DecreasePlayerCapacity() => SetCapacity(-_measurementError);

        private void Move(Vector2 moveInput) => _playerMovement.Direction = moveInput;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                SetCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                SetCapacity(-comet.Capacity);
                _cometsSpawnerLogics.CreateComet(comet);
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                if (Capacity > enemy.Capacity)
                {
                    SetCapacity(enemy.Capacity * 100f);
                    enemy.gameObject.SetActive(false);
                }
                else
                {
                    SetCapacity(-enemy.Capacity * 100f);
                }
            }
        }
    }
}