using Unity.Netcode;

namespace Planet_IO
{
    public class Point : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
    }
}