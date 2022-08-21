using UnityEngine;
using Zenject;

namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        [Header("Borders")] [SerializeField] private BordersTrigger _bordersTrigger;

        [Header("player script")] [SerializeField]
        private PlayerScale _playerScale;

        [Header("Spawner")] private CometsSpawnerLogic _cometsSpawnerLogic;
        private LogicsPointsSpawner _logicsPointsSpawner;

        [Inject]
        private void Construct(CometsSpawnerLogic cometsSpawnerLogic, LogicsPointsSpawner logicsPointsSpawner)
        {
            _cometsSpawnerLogic = cometsSpawnerLogic;
            _logicsPointsSpawner = logicsPointsSpawner;
        }

        private void OnEnable() => _bordersTrigger.OnPlayerTriggeredHandler += _playerScale.DecreasePlayerCapacity;

        private void OnDisable() => _bordersTrigger.OnPlayerTriggeredHandler -= _playerScale.DecreasePlayerCapacity;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                _playerScale.IncreasePlayerCapacity(point.Capacity);
                _logicsPointsSpawner.CreatePoint(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _playerScale.IncreasePlayerCapacity(-comet.Capacity);
                _cometsSpawnerLogic.CreateComet(comet);
            }
        }
    }
}