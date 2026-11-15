namespace PlanetIO
{
    public enum DailyModifier : byte
    {
        None,
        LowGravity,
        HeavyGravity,
        GoldRush
    }

    public static class DailyModifierRules
    {
        public const float LowGravityScale = 0.5f;
        public const float HeavyGravityScale = 1.5f;
        public const float GoldRushLootScale = 2f;

        public static DailyModifier ForDay(int dayNumber)
        {
            var random = new System.Random(dayNumber * 31 + 5);
            return random.Next(5) switch
            {
                2 => DailyModifier.LowGravity,
                3 => DailyModifier.HeavyGravity,
                4 => DailyModifier.GoldRush,
                _ => DailyModifier.None
            };
        }

        public static float GravityScale(DailyModifier modifier) => modifier switch
        {
            DailyModifier.LowGravity => LowGravityScale,
            DailyModifier.HeavyGravity => HeavyGravityScale,
            _ => 1f
        };

        public static float LootScale(DailyModifier modifier) =>
            modifier == DailyModifier.GoldRush ? GoldRushLootScale : 1f;
    }
}
