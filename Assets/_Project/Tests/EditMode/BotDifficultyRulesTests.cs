using NUnit.Framework;
using System.Collections.Generic;
using Unity.Services.Multiplayer;
using PlanetIO.Infrastructure.Networking;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class BotDifficultyRulesTests
    {
        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(int.MaxValue)]
        public void InvalidPreferenceFallsBackToMedium(int value)
        {
            Assert.That(BotDifficultyRules.Normalize(value), Is.EqualTo(1));
        }

        [TestCase(NetworkSessionMode.Host)]
        [TestCase(NetworkSessionMode.Client)]
        [TestCase(NetworkSessionMode.None)]
        public void OnlineModeIgnoresLocalDifficulty(NetworkSessionMode mode)
        {
            Assert.That(BotDifficultyRules.Resolve(0, mode), Is.EqualTo(BotDifficulty.Medium));
            Assert.That(BotDifficultyRules.Resolve(2, mode), Is.EqualTo(BotDifficulty.Medium));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SoloUsesSelectedDifficulty(int value)
        {
            Assert.That((int)BotDifficultyRules.Resolve(value, NetworkSessionMode.SinglePlayer), Is.EqualTo(value));
        }

        [TestCase(NetworkSessionMode.Host)]
        [TestCase(NetworkSessionMode.Client)]
        public void OnlineBotsUseRoomDifficultyWhilePlayersKeepNormalDamage(NetworkSessionMode mode)
        {
            Assert.That(BotDifficultyRules.Resolve(0, mode, BotDifficulty.Hard), Is.EqualTo(BotDifficulty.Hard));
            Assert.That(BotDifficultyRules.Resolve(2, mode, BotDifficulty.Easy), Is.EqualTo(BotDifficulty.Easy));
            foreach (BotDifficulty difficulty in new[] { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard })
                Assert.That(BotDifficultyRules.PlayerIncomingDamage(difficulty, mode), Is.EqualTo(1f));
        }

        [Test]
        public void SoloDamageAndBotAttributesScaleWithDifficulty()
        {
            Assert.That(BotDifficultyRules.PlayerIncomingDamage(BotDifficulty.Easy, NetworkSessionMode.SinglePlayer), Is.LessThan(1f));
            Assert.That(BotDifficultyRules.PlayerIncomingDamage(BotDifficulty.Hard, NetworkSessionMode.SinglePlayer), Is.GreaterThan(1f));
            Assert.That(BotDifficultyRules.BotIncomingDamage(BotDifficulty.Easy), Is.GreaterThan(1f));
            Assert.That(BotDifficultyRules.BotIncomingDamage(BotDifficulty.Hard), Is.LessThan(1f));
            Assert.That(BotDifficultyRules.BotOutgoingDamage(BotDifficulty.Easy), Is.LessThan(1f));
            Assert.That(BotDifficultyRules.BotOutgoingDamage(BotDifficulty.Hard), Is.GreaterThan(1f));
        }

        [TestCase("0", BotDifficulty.Easy)]
        [TestCase("2", BotDifficulty.Hard)]
        [TestCase("bad", BotDifficulty.Medium)]
        [TestCase("99", BotDifficulty.Medium)]
        public void RoomMetadataValidatesSharedDifficulty(string value, BotDifficulty expected)
        {
            var properties = new Dictionary<string, SessionProperty>
            {
                [UgsSessionConnector.DifficultyProperty] = new SessionProperty(value)
            };
            Assert.That(UgsSessionConnector.ReadDifficulty(properties), Is.EqualTo(expected));
            Assert.That(UgsSessionConnector.ReadDifficulty(null), Is.EqualTo(BotDifficulty.Medium));
            Assert.That(RoomConnectionSettings.Default.Difficulty, Is.EqualTo(BotDifficulty.Medium));
        }

        [Test]
        public void HardReactsEarlierAndPredictsFartherThanEasy()
        {
            var easy = BotDifficultyRules.For(BotDifficulty.Easy);
            var medium = BotDifficultyRules.For(BotDifficulty.Medium);
            var hard = BotDifficultyRules.For(BotDifficulty.Hard);
            Assert.That(hard.ReactionSeconds, Is.LessThan(medium.ReactionSeconds));
            Assert.That(medium.ReactionSeconds, Is.LessThan(easy.ReactionSeconds));
            Assert.That(hard.PredictionSeconds, Is.GreaterThan(medium.PredictionSeconds));
            Assert.That(medium.PredictionSeconds, Is.GreaterThan(easy.PredictionSeconds));
        }

        [TestCase(BotDifficulty.Easy)]
        [TestCase(BotDifficulty.Medium)]
        [TestCase(BotDifficulty.Hard)]
        public void BoostNeverSpendsStartingMassOrAcceleratesIntoWrongDirection(BotDifficulty difficulty)
        {
            Assert.That(BotDifficultyRules.ShouldBoost(difficulty, true, false, 0.084f, 0.08f, 0f, 5f, 1f), Is.False);
            Assert.That(BotDifficultyRules.ShouldBoost(difficulty, true, false, 1f, 0.08f, 0f, 5f, -1f), Is.False);
            Assert.That(BotDifficultyRules.ShouldBoost(difficulty, true, false, 1f, 0.08f, 0f, 5f, 1f), Is.True);
        }

        [Test]
        public void HuntingBoostPreservesEdibleAdvantageForEntireBurst()
        {
            Assert.That(BotDifficultyRules.ShouldBoost(BotDifficulty.Hard, false, true, 1.09f, 0.08f, 1f, 5f, 1f), Is.False);
            Assert.That(BotDifficultyRules.ShouldBoost(BotDifficulty.Hard, false, true, 1.2f, 0.08f, 1f, 5f, 1f), Is.True);
            Assert.That(BotDifficultyRules.ShouldBoost(BotDifficulty.Easy, false, true, 2f, 0.08f, 1f, 5f, 1f), Is.False);
        }

        [Test]
        public void PredictionAndBorderSteeringStayBounded()
        {
            Assert.That(BotDifficultyRules.PursuitPoint(Vector2.zero, Vector2.right * 100f, 1f), Is.EqualTo(Vector2.right * 6f));
            Rect bounds = new(-10f, -10f, 20f, 20f);
            Assert.That(BotDifficultyRules.BorderAvoidance(new Vector2(9f, 0f), bounds, 2f), Is.EqualTo(Vector2.left));
            Assert.That(BotDifficultyRules.BorderAvoidance(Vector2.zero, bounds, 2f), Is.EqualTo(Vector2.zero));
        }
    }
}
