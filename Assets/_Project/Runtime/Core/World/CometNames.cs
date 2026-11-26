namespace PlanetIO
{
    public static class CometNames
    {
        private static readonly string[] Names =
        {
            "Halley", "Hale-Bopp", "Hyakutake", "Shoemaker-Levy 9", "NEOWISE", "Swift-Tuttle", "Encke", "Tempel 1", "Wild 2", "Churyumov",
            "Hartley 2", "Lovejoy", "ISON", "McNaught", "West", "Ikeya-Seki", "Kohoutek", "Donati", "Biela", "Holmes",
            "Pons-Brooks", "Tempel-Tuttle", "Giacobini-Zinner", "Borisov", "Machholz", "Swan", "PanSTARRS", "Lemmon", "Siding Spring", "Wirtanen",
            "Tsuchinshan-ATLAS", "Leonard", "ATLAS", "Bernardinelli", "Borrelly", "Grigg-Skjellerup", "Kopff", "Arend-Roland", "Bennett", "Tebbutt",
            "Coggia", "Whipple", "Mrkos", "Humason", "Austin", "Levy", "Tuttle", "Pons-Winnecke", "Finlay", "Schwassmann",
            "Daniel", "Brorsen-Metcalf", "Denning", "Crommelin", "Faye", "Gale", "Giclas", "Barnard", "Brooks", "Swift",
            "Kowal", "Kearns-Kwee", "Neujmin", "Taylor", "Zhu-Balam", "Bradfield", "Cernis", "Mellish", "Lulin", "Elenin",
            "Garradd", "Hergenrother", "Catalina", "Stephan-Oterma", "Ashbrook-Jackson", "Comas Sola", "Wolf", "Johnson", "Tritton", "Harrington",
            "Gehrels", "Hug-Bell", "Meier", "Morehouse", "Okabayashi", "Peltier", "Pojmanski", "Rudnicki", "Schaumasse", "Skjellerup",
            "Takamizawa", "Thiele", "Tombaugh", "Sugano", "Fujikawa", "Tago-Sato-Kosaka", "Kudo-Fujikawa", "Seki-Lines", "Burnham", "Dodwell"
        };

        public static int Count => Names.Length;

        public static string Get(int index) => Names[index];

        public static string Get(ulong seed) => Names[(int)(seed % (ulong)Names.Length)];
    }
}
