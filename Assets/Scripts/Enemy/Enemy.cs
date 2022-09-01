using Dythervin.AutoAttach;
using UnityEngine;
using Zenject;

namespace Planet_IO
{
    [RequireComponent(typeof(EnemyScale))]
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy script")]
        [SerializeField, Attach] private EnemyScale _enemyScale;

        [Header("Spawner")] 
        private CometsSpawnerLogics _cometsSpawnerLogics;
        private PointsSpawnerLogics _pointsSpawnerLogics;

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
                _enemyScale.SetEnemyCapacity(point.Capacity);
                _pointsSpawnerLogics.CreatePoint(point);
            }
            else if (col.TryGetComponent(out Comet comet))
            {
                _enemyScale.SetEnemyCapacity(-comet.Capacity);
                _cometsSpawnerLogics.CreateComet(comet);
            }
        }
    }
}
