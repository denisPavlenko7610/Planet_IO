using UnityEngine;
using Zenject;
using Dythervin.AutoAttach;


namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : Planet
    {
        private const float _measurementError = 0.01f;
        
        [Header("Borders")] 
        [SerializeField, Attach(Attach.Scene)] private BordersTrigger _bordersTrigger;

        [Header("player script")]
        [SerializeField, Attach] private InputPlayerSystem _inputPlayerSystem;
        [SerializeField, Attach] private PlayerMovement _playerMovement;

        [Header("Spawner")] 
        [SerializeField] private Transform _pointSpawnTransform;

        [Inject]
        private void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnEnable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler += _scale.DecreaseCapacity;
            _inputPlayerSystem.Input += Move;
        }

        private void OnDisable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler -= _scale.DecreaseCapacity;
            _inputPlayerSystem.Input -= Move;
        } 
        
        public void EnableBoost()
        {
            if (_scale.Capacity > _scale.MinCapacity + _measurementError)
            {
                DecreasePlayerCapacity();
                CreatePoints();
            }
        }

        private void CreatePoints() => _pointsSpawnerLogics.CreatePoint(_pointSpawnTransform);

        private void DecreasePlayerCapacity() => _scale.SetCapacity(-_measurementError);

        private void Move(Vector2 moveInput) => _playerMovement.Direction = moveInput;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                _scale.SetCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _scale.SetCapacity(-comet.Capacity);
                _cometsSpawnerLogics.CreateComet(comet);
            }else if(other.TryGetComponent(out EnemyScale enemy))
            {
                if (_scale.Capacity > enemy.Capacity)
                {
                    _scale.SetCapacity(enemy.Capacity * 100f);
                    enemy.gameObject.SetActive(false);
                }
                else
                    _scale.SetCapacity(-enemy.Capacity * 100f);
            }
        }
    }
}