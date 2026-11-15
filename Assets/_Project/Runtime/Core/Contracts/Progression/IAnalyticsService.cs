namespace PlanetIO
{
    public interface IAnalyticsService
    {
        void Track(string eventName, params (string Key, object Value)[] parameters);
    }

    public static class AnalyticsEvents
    {
        public const string SessionStart = "session_start";
        public const string RunStart = "run_start";
        public const string RunEnd = "run_end";
        public const string TierUp = "tier_up";
        public const string PerkOffered = "perk_offered";
        public const string PerkPicked = "perk_picked";
        public const string AdOffered = "ad_offered";
        public const string AdResult = "ad_result";
        public const string TutorialStep = "tutorial_step";
        public const string MissionComplete = "mission_complete";
        public const string ShopPurchase = "shop_purchase";
        public const string EventStart = "event_start";
    }
}
