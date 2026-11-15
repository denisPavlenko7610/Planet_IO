namespace PlanetIO
{
    public static class WorldSeedRules
    {
        public static int FromDay(int dayNumber)
        {
            unchecked
            {
                return dayNumber * 2654435 + 12345;
            }
        }
    }
}
