using Unity.Netcode;

namespace PlanetIO
{
    public sealed class Comet : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
    }
}
