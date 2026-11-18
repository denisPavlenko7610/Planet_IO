namespace PlanetIO
{
    public static class ReviewPromptRules
    {
        public const int MinRuns = 5;
        public const int MinDaysSinceFirstRun = 2;
        public const int MaxPrompts = 3;
        public const int MinDaysBetweenPrompts = 90;

        public static bool ShouldPrompt(ServiceState state, bool rankedUp, int today)
        {
            if (state == null || !rankedUp || state.RunsFinished < MinRuns)
            {
                return false;
            }

            if (state.FirstRunDay <= 0 || today - state.FirstRunDay < MinDaysSinceFirstRun)
            {
                return false;
            }

            if (state.ReviewPrompts >= MaxPrompts)
            {
                return false;
            }

            return state.ReviewPrompts == 0 || today - state.LastReviewDay >= MinDaysBetweenPrompts;
        }
    }
}
