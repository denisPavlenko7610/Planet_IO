using UnityEngine;
using Zenject;

namespace Planet_IO
{
    [RequireComponent(typeof(EnemyScale))]
    public class Enemy : Planet
    {
        [Inject]
        private void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out Point point))
            {  
                _scale.IncreaseCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (col.TryGetComponent(out Comet comet))
            {
                _scale.IncreaseCapacity(-comet.Capacity);
                _cometsSpawnerLogics.CreateComet(comet);
            }
        }
    }
}
