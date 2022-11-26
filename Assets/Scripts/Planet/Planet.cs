using RDTools.AutoAttach;
using UnityEngine;
using Zenject;

namespace Planet_IO
{
    public abstract class Planet : MonoBehaviour
    {
        [SerializeField, Attach] protected PlanetScale _scale;
        [SerializeField, Attach] protected SpriteRenderer _spriteRenderer;
        
        [Header("Spawner")] 
        protected CometsSpawnerLogics _cometsSpawnerLogics;
        protected PointsSpawnerLogics _pointsSpawnerLogics;
        
        
        
        [Inject]
        private void Construct(CometsSpawnerLogics cometsSpawnerLogics, PointsSpawnerLogics pointsSpawnerLogics)
        {
            _cometsSpawnerLogics = cometsSpawnerLogics;
            _pointsSpawnerLogics = pointsSpawnerLogics;
        }
    }
}