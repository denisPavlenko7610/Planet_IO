using Unity.Netcode;

namespace PlanetIO
{
    public sealed class Point : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
    }
}
