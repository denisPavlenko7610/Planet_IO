using Unity.Netcode;
using UnityEngine;

namespace PlanetIO
{
    public static class OrbitClock
    {
        private static double _migrationOffset;
        public static void SetMigrationOffset(double offset) => _migrationOffset = offset;
        public static double MigrationOffset => _migrationOffset;
        public static double Now
        {
            get
            {
                NetworkManager manager = NetworkManager.Singleton;
                return manager != null && manager.IsListening ? manager.ServerTime.Time + _migrationOffset : Time.timeAsDouble;
            }
        }
    }
}
