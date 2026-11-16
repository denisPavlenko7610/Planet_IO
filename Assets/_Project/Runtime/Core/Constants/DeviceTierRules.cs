namespace PlanetIO
{
    public enum DeviceTier
    {
        Standard,
        Low
    }

    public static class DeviceTierRules
    {
        public const int LowMemoryMegabytes = 3500;
        public const int LowProcessorCount = 4;
        public const int LowQualityLevel = 0;
        public const int StandardQualityLevel = 1;

        public static DeviceTier Resolve(int systemMemoryMegabytes, int processorCount)
        {
            bool lowMemory = systemMemoryMegabytes > 0 && systemMemoryMegabytes <= LowMemoryMegabytes;
            bool fewCores = processorCount > 0 && processorCount <= LowProcessorCount;
            return lowMemory || fewCores ? DeviceTier.Low : DeviceTier.Standard;
        }

        public static int QualityLevel(DeviceTier tier, int levelCount)
        {
            int level = tier == DeviceTier.Low ? LowQualityLevel : StandardQualityLevel;
            return levelCount > level ? level : levelCount - 1;
        }
    }
}
