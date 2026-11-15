namespace PlanetIO
{
    public readonly struct MissionView
    {
        public readonly int Definition;
        public readonly int Progress;
        public readonly int Target;
        public readonly int Reward;
        public readonly bool Complete;
        public readonly bool Claimed;

        public MissionView(int definition, int progress, int target, int reward, bool complete, bool claimed)
        {
            Definition = definition;
            Progress = progress;
            Target = target;
            Reward = reward;
            Complete = complete;
            Claimed = claimed;
        }
    }
}
