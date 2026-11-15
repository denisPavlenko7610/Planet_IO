using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    [AutoStaticsCleanup]
    public static partial class PlanetDirectory
    {
        public const float LeaderMinimumCapacity = 0.4f;
        private const float LeaderRefreshSeconds = 0.5f;

        private static readonly List<PlanetScale> Planets = new();
        private static float _nextLeaderRefresh;
        private static ulong _leaderId = ulong.MaxValue;

        public static IReadOnlyList<PlanetScale> All => Planets;

        public static void Register(PlanetScale planet)
        {
            if (planet != null && !Planets.Contains(planet))
            {
                Planets.Add(planet);
            }
        }

        public static void Unregister(PlanetScale planet)
        {
            Planets.Remove(planet);
        }

        public static ulong ResolveLeaderId()
        {
            if (Time.time < _nextLeaderRefresh)
            {
                return _leaderId;
            }

            _nextLeaderRefresh = Time.time + LeaderRefreshSeconds;
            _leaderId = FindLeaderId();
            return _leaderId;
        }

        private static ulong FindLeaderId()
        {
            PlanetScale leader = null;
            for (int index = 0; index < Planets.Count; index++)
            {
                PlanetScale planet = Planets[index];
                if (planet == null ||
                    !planet.isActiveAndEnabled ||
                    !planet.IsSpawned ||
                    !planet.AreMoonsVisible ||
                    planet.Capacity < LeaderMinimumCapacity)
                {
                    continue;
                }

                if (leader == null ||
                    planet.Capacity > leader.Capacity ||
                    planet.Capacity == leader.Capacity && planet.NetworkObjectId < leader.NetworkObjectId)
                {
                    leader = planet;
                }
            }

            return leader != null ? leader.NetworkObjectId : ulong.MaxValue;
        }
    }
}
