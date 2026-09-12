using Unity.Netcode;

namespace PlanetIO
{
    public sealed class Food : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
        public bool IsDropped { get; private set; }

        public int LifecycleVersion { get; private set; }

        private bool _isClaimed;

        public bool TryClaim()
        {
            if (_isClaimed)
            {
                return false;
            }

            _isClaimed = true;
            return true;
        }

        public void ResetClaim()
        {
            _isClaimed = false;
        }

        public int MarkAsDropped()
        {
            IsDropped = true;
            return ++LifecycleVersion;
        }

        public void MarkAsStored()
        {
            IsDropped = false;
            LifecycleVersion++;
        }
    }
}
