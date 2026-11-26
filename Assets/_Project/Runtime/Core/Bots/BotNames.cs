namespace PlanetIO
{
    public static class BotNames
    {
        private static readonly string[] Names =
        {
            "Nova", "Orbit", "Pulsar", "Quasar", "Nebula", "Comet", "Meteor", "Luna", "Sol", "Vega",
            "Sirius", "Rigel", "Atlas", "Titan", "Io", "Europa", "Callisto", "Phobos", "Deimos", "Ceres",
            "Juno", "Kepler", "Hubble", "Gagarin", "Laika", "Apollo", "Zenith", "Nadir", "Eclipse", "Aurora",
            "Stardust", "Photon", "Neutron", "Plasma", "Gravity", "Void", "Cosmo", "Astro", "Rocket", "Saturn"
        };

        public static string Get(ulong seed) => Names[(int)(seed % (ulong)Names.Length)];
    }
}
