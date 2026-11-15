using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public static class OrbitClock
    {
        public static double Now
        {
            get
            {
                NetworkManager manager = NetworkManager.Singleton;
                return manager != null && manager.IsListening ? manager.ServerTime.Time : Time.timeAsDouble;
            }
        }
    }
}
