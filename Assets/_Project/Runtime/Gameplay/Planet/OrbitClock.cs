using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public static class OrbitClock
    {
		private static double? _soloOffset;

		public static void UseSoloTime(bool enabled)
		{
			_soloOffset = null;
			if (enabled)
			{
				_soloOffset = Now - Time.timeAsDouble;
			}
		}

        public static double Now
        {
            get
            {
				if (_soloOffset is double offset)
				{
					return Time.timeAsDouble + offset;
				}

                NetworkManager manager = NetworkManager.Singleton;
                return manager != null && manager.IsListening ? manager.ServerTime.Time : Time.timeAsDouble;
            }
        }
    }
}
