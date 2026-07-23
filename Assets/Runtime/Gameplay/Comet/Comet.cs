using Unity.Netcode;

namespace Planet_IO
{
    public sealed class Comet : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
    }
}
