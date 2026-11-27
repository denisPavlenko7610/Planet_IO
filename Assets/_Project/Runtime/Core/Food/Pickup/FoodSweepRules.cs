namespace PlanetIO
{
    public static class FoodSweepRules
    {
        public const float Interval = 0.1f;

        public static bool Advance(ref float timer, float deltaTime)
        {
            timer += deltaTime;
            if (timer < Interval)
            {
                return false;
            }

            timer %= Interval;
            return true;
        }
    }
}
