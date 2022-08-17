using UnityEngine;
using Zenject;

namespace PlanetIO
{
    public class Borders : MonoBehaviour
    {
        private LogicsCometsSpawner _logicsCometsSpawner;
        [SerializeField] private BordersTrigger _bordersTrigger;

        [Inject]
        private void Construct(LogicsCometsSpawner logicsCometsSpawner) => _logicsCometsSpawner = logicsCometsSpawner;

        private void OnEnable() => _bordersTrigger.OnCometTriggeredHandler += _logicsCometsSpawner.CreateComet;

        private void OnDisable() => _bordersTrigger.OnCometTriggeredHandler -= _logicsCometsSpawner.CreateComet;
    }
}