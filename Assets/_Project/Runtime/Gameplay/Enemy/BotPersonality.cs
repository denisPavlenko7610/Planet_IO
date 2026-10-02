namespace PlanetIO
{
    public enum BotPersonality : byte
    {
        Cautious,
        Balanced,
        Hunter
    }

    public readonly struct BotTuning
    {
        public BotTuning(float awareness, float huntRatio, float threatRatio, float speed)
        {
            Awareness = awareness;
            HuntRatio = huntRatio;
            ThreatRatio = threatRatio;
            Speed = speed;
        }

        public float Awareness { get; }
        public float HuntRatio { get; }
        public float ThreatRatio { get; }
        public float Speed { get; }

        public static BotPersonality PersonalityFor(ulong seed) => (BotPersonality)((seed * 2654435761UL >> 16) % 3);

        public static BotTuning For(BotPersonality personality) => personality switch
        {
            BotPersonality.Cautious => new BotTuning(0.8f, 1.35f, 0.95f, 0.95f),
            BotPersonality.Hunter => new BotTuning(1.35f, 1.05f, 1.25f, 1.08f),
            _ => new BotTuning(1f, 1f, 1f, 1f)
        };
    }
}
