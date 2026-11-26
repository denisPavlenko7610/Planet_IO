using System;

namespace PlanetIO
{
    public static class PlanetNames
    {
        private static readonly string[] Names =
        {
            "Earth", "Mars", "Venus", "Jupiter", "Saturn", "Mercury", "Neptune", "Uranus", "Pluto", "Luna",
            "Titan", "Europa", "Io", "Ganymede", "Callisto", "Enceladus", "Triton", "Phobos", "Deimos", "Charon",
            "Ceres", "Eris", "Haumea", "Makemake", "Sedna", "Quaoar", "Orcus", "Gonggong", "Varuna", "Ixion",
            "Proxima b", "Kepler-452b", "Kepler-22b", "Kepler-186f", "TRAPPIST-1e", "TRAPPIST-1d", "TRAPPIST-1f", "TRAPPIST-1g", "Gliese 581g", "Gliese 667Cc",
            "51 Pegasi b", "Osiris", "Tau Ceti e", "Barnard b", "Teegarden b", "LHS 1140 b", "K2-18b", "Kepler-62f", "Kepler-442b",
            "Kepler-1649c", "Ross 128 b", "Wolf 1069 b", "Beta Pictoris b", "HR 8799 e", "Kepler-16b", "Kepler-10b", "Kepler-69c", "Kepler-296e",
            "Tatooine", "Krypton", "Arrakis", "Pandora", "Naboo", "Endor", "Hoth", "Alderaan", "Coruscant", "Dagobah",
            "Mustafar", "Kamino", "Jakku", "Vulcan", "Gallifrey", "Skaro", "Trantor", "Solaris", "Cybertron", "Namek",
            "Asgard", "Midgard", "Magrathea", "Caprica", "Kobol", "Pern", "Mongo", "Apokolips", "Xandar", "Vormir",
            "Zebes", "Corneria", "Eden", "Aurora", "Terra", "Gaia", "Nibiru", "Caladan", "Tralfamadore", "Bespin",
            "Mandalore", "Dantooine", "Yavin", "Kashyyyk", "Geonosis", "Utapau", "Kessel", "Scarif", "Lothal", "Exegol",
            "Rhea", "Iapetus", "Dione", "Tethys", "Mimas", "Hyperion", "Phoebe", "Janus", "Epimetheus", "Prometheus",
            "Atlas", "Pan", "Amalthea", "Himalia", "Thebe", "Adrastea", "Metis", "Elara", "Nereid", "Proteus",
            "Larissa", "Galatea", "Despina", "Thalassa", "Naiad", "Puck", "Titania", "Oberon", "Miranda", "Ariel",
            "Umbriel", "Cordelia", "Ophelia", "Bianca", "Cressida", "Desdemona", "Juliet", "Portia", "Rosalind", "Belinda",
            "Perdita", "Mab", "Cupid", "Nix", "Hydra", "Kerberos", "Styx", "Hiiaka", "Namaka", "Weywot",
            "Vesta", "Pallas", "Juno", "Hygiea", "Eros", "Ida", "Gaspra", "Bennu", "Ryugu", "Itokawa",
            "Psyche", "Lutetia", "Mathilde", "Arrokoth", "Apophis", "Didymos", "Dimorphos", "Steins", "Toutatis", "Eunomia",
            "Dimidium", "Poltergeist", "Draugr", "Phobetor", "Dagon", "Thestias", "Taphao Thong", "Taphao Kaew", "Veritate", "Spe",
            "Galileo", "Brahe", "Lipperhey", "Harriot", "Amateru", "Hypatia", "Eburonia", "Arion", "Janssen", "Alpha Centauri Bb",
            "Gliese 876 d", "Gliese 436 b", "GJ 1214 b", "WASP-12b", "WASP-121b", "HD 189733 b"
        };

        public static int Count => Names.Length;

        public static string Get(int index) => Names[index];

        public static string Pick(Random random, string excluded = null)
        {
            string name;
            do
            {
                name = Names[random.Next(Names.Length)];
            }
            while (name == excluded);

            return name;
        }
    }
}
