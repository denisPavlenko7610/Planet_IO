using UnityEngine;
using Zenject;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        [Header("Borders")] [SerializeField] private BordersTrigger _bordersTrigger;

        [Header("player script")] [SerializeField]
        private PlayerScale _playerScale;

        [Header("Spawner")] private CometsSpawnerLogic cometsSpawnerLogic;
        private LogicsPointsSpawner _logicsPointsSpawner;

        [Inject]
        private void Construct(CometsSpawnerLogic cometsSpawnerLogic, LogicsPointsSpawner logicsPointsSpawner)
        {
            this.cometsSpawnerLogic = cometsSpawnerLogic;
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
                cometsSpawnerLogic.CreateComet(comet);
            }
        }
    }
}