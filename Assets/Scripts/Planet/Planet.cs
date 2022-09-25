using Dythervin.AutoAttach;
using UnityEngine;

namespace Planet_IO
{
    public abstract class Planet : MonoBehaviour
    {
        [SerializeField, Attach] protected PlanetScale _scale;
        
        [Header("Spawner")] 
        protected CometsSpawnerLogics _cometsSpawnerLogics;
        protected PointsSpawnerLogics _pointsSpawnerLogics;
    }
}