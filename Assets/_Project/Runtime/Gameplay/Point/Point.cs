using Unity.Netcode;

namespace PlanetIO
{
    public sealed class Point : NetworkBehaviour, ICapacity
    {
        public float Capacity { get; set; }
		public bool IsDropped { get; private set; }

		public int LifecycleVersion { get; private set; }

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
