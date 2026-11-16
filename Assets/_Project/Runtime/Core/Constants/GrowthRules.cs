namespace PlanetIO
{
    public static class GrowthRules
    {
        public static float FoodGrowth(float capacity, float eaterMultiplier, float valueMultiplier, float nutrition)
        {
            return nutrition > 0f ? nutrition : capacity * eaterMultiplier * valueMultiplier * 2f;
        }

        public static float ApplyFalloff(float growth, float currentCapacity, float falloff, bool isDroppedMass)
        {
            if (isDroppedMass || falloff <= 0f || currentCapacity <= 0f)
            {
                return growth;
            }

            return growth / (1f + currentCapacity * falloff);
        }
    }
}