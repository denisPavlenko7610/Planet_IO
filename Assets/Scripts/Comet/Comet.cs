using Unity.Netcode;

namespace Planet_IO
{
    public class Comet : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
    }
}