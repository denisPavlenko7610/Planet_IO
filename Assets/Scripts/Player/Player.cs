using Cinemachine;
using Pool;
using Spawner;
using UnityEngine;
using Zenject;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        [Header("Borders")] 
        [SerializeField] private BordersTrigger _bordersTrigger;
        
        [Header("player script")]
        [SerializeField] private PlayerScale _playerScale;
        
        [Header("Spawner")]
        private LogicsCometsSpawner _logicsCometsSpawner;
        private LogicsPointsSpawner _logicsPointsSpawner;

        [Inject]
        private void Construct(LogicsCometsSpawner logicsCometsSpawner,LogicsPointsSpawner logicsPointsSpawner )
        {
           
            _logicsCometsSpawner = logicsCometsSpawner;
            _logicsPointsSpawner = logicsPointsSpawner;
        }

        private void OnEnable() => _bordersTrigger.OnPlayerTriggeredHandler += _playerScale.BordersInteraction;

        private void OnDisable() => _bordersTrigger.OnPlayerTriggeredHandler -= _playerScale.BordersInteraction;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                _playerScale.NewScalePlayer(point.Capacity);
                _logicsPointsSpawner.CreatePoint(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _playerScale.NewScalePlayer(-comet.Capacity);
                _logicsCometsSpawner.CreateComet(comet);
            }
        }
    }
}