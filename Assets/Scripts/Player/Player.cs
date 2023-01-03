using Planet_IO.Utils;
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

        [Header("Spawner")] 
        [SerializeField] private Transform _pointSpawnTransform;
        
        [Header("Spawner")] 
        private CometsSpawnerLogics _cometsSpawnerLogics;
        private PointsSpawnerLogics _pointsSpawnerLogics;

        private Vector3 movementVector;

        [Inject]
        public void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnEnable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler += DecreaseCapacity;
        }

        private void OnDisable()
        {
            _bordersTrigger.OnPlayerTriggeredHandler -= DecreaseCapacity;
        }

        public void EnableBoost()
        {
            if (!(Capacity > MinCapacity + Constants.MeasurementError))
                return;
            
            DecreasePlayerCapacity();
            CreatePoints();
        }

        private void CreatePoints() => _pointsSpawnerLogics.CreatePoint(_pointSpawnTransform);

        private void DecreasePlayerCapacity() => SetCapacity(-Constants.MeasurementError);

       

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
                    SetCapacity(enemy.Capacity * Constants.CapacityMultValue);
                    enemy.gameObject.SetActive(false);
                }
                else
                {
                    SetCapacity(-enemy.Capacity * Constants.CapacityMultValue);
                }
            }
        }
    }
}