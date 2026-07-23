using Unity.Netcode;

namespace Planet_IO
{
    public sealed class Point : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
    }
}
