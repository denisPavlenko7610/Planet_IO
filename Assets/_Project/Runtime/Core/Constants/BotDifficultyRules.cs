using UnityEngine;

namespace PlanetIO
{
    public enum BotDifficulty : byte { Easy, Medium, Hard }

    public readonly struct BotDifficultyTuning
    {
        public readonly float ReactionSeconds;
        public readonly float Awareness;
        public readonly float HuntRatio;
        public readonly float PredictionSeconds;
        public readonly float BoostSeconds;
        public readonly float BoostCooldown;

        public BotDifficultyTuning(float reaction, float awareness, float huntRatio, float prediction, float boost, float cooldown)
        {
            ReactionSeconds = reaction;
            Awareness = awareness;
            HuntRatio = huntRatio;
            PredictionSeconds = prediction;
            BoostSeconds = boost;
            BoostCooldown = cooldown;
        }
    }

    public static class BotDifficultyRules
    {
        public const float BoostCost = 0.004f;
        public const float BoostTickSeconds = 0.15f;
        public const float BoostSpeedMultiplier = 2f;
        public const float BoostFoodFraction = 0.8f;

        public static int Normalize(int value) => value is >= 0 and <= 2 ? value : (int)BotDifficulty.Medium;

        public static BotDifficulty Resolve(int preference, NetworkSessionMode mode) => mode == NetworkSessionMode.SinglePlayer
            ? (BotDifficulty)Normalize(preference) : BotDifficulty.Medium;

        public static BotDifficulty Resolve(int preference, NetworkSessionMode mode, BotDifficulty roomDifficulty) =>
            mode == NetworkSessionMode.SinglePlayer ? (BotDifficulty)Normalize(preference) :
            mode is NetworkSessionMode.Host or NetworkSessionMode.Client
                ? (BotDifficulty)Normalize((int)roomDifficulty) : BotDifficulty.Medium;

        public static float PlayerIncomingDamage(BotDifficulty difficulty, NetworkSessionMode mode) =>
            mode == NetworkSessionMode.SinglePlayer ? difficulty switch
            {
                BotDifficulty.Easy => 0.75f,
                BotDifficulty.Hard => 1.25f,
                _ => 1f
            } : 1f;

        public static float BotIncomingDamage(BotDifficulty difficulty) => difficulty switch
        {
            BotDifficulty.Easy => 1.25f,
            BotDifficulty.Hard => 0.85f,
            _ => 1f
        };

        public static float BotOutgoingDamage(BotDifficulty difficulty) => difficulty switch
        {
            BotDifficulty.Easy => 0.8f,
            BotDifficulty.Hard => 1.2f,
            _ => 1f
        };

        public static BotDifficultyTuning For(BotDifficulty difficulty) => difficulty switch
        {
            BotDifficulty.Easy => new(0.75f, 0.8f, 1.28f, 0f, 0.35f, 8f),
            BotDifficulty.Hard => new(0.2f, 1.25f, 1.08f, 0.55f, 1f, 3f),
            _ => new(0.4f, 1f, 1.16f, 0.25f, 0.65f, 5f)
        };

        public static bool CanAffordBoost(float capacity, float startingMass) => capacity > startingMass + BoostCost;

        public static bool ShouldBoost(BotDifficulty difficulty, bool evading, bool hunting, float capacity,
            float startingMass, float targetMass, float distanceToTarget, float alignment)
        {
            if (!CanAffordBoost(capacity, startingMass) || alignment < 0.7f)
                return false;
            if (evading)
                return true;
            if (!hunting || difficulty == BotDifficulty.Easy || distanceToTarget < 1.5f)
                return false;
            float budget = Mathf.Ceil(For(difficulty).BoostSeconds / BoostTickSeconds) * BoostCost;
            return capacity - budget >= targetMass * CombatRules.DefaultEatSizeRatio;
        }

        public static Vector2 PursuitPoint(Vector2 target, Vector2 velocity, float predictionSeconds) =>
            target + Vector2.ClampMagnitude(velocity * Mathf.Max(0f, predictionSeconds), 6f);

        public static Vector2 BorderAvoidance(Vector2 position, Rect bounds, float margin)
        {
            return new Vector2(
                position.x < bounds.xMin + margin ? 1f : position.x > bounds.xMax - margin ? -1f : 0f,
                position.y < bounds.yMin + margin ? 1f : position.y > bounds.yMax - margin ? -1f : 0f).normalized;
        }
    }
}
