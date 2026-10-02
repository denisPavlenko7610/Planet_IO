using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlanetIO.Tests
{
    public sealed class GameRulesExtensionsTests
    {
        [Test]
        public void Growth_FalloffSlowsBigPlanets()
        {
            float small = GrowthRules.ApplyFalloff(0.01f, 0.1f, 1f, false);
            float big = GrowthRules.ApplyFalloff(0.01f, 1.5f, 1f, false);

            Assert.That(small, Is.GreaterThan(big));
            Assert.That(big, Is.EqualTo(0.004f).Within(1e-6f));
        }

        [Test]
        public void Growth_DroppedMassIgnoresFalloff()
        {
            Assert.That(GrowthRules.ApplyFalloff(0.05f, 1.5f, 1f, true), Is.EqualTo(0.05f));
        }

        [Test]
        public void WorldBounds_StopsVelocityPushingOutward()
        {
            Rect bounds = Constants.WorldBounds;
            Vector2 atRightEdge = new(bounds.xMax, 0f);

            Vector2 velocity = WorldBounds.KeepInside(atRightEdge, new Vector2(3f, 2f));

            Assert.That(velocity, Is.EqualTo(new Vector2(0f, 2f)));
            Assert.That(WorldBounds.KeepInside(atRightEdge, new Vector2(-3f, 0f)), Is.EqualTo(new Vector2(-3f, 0f)));
        }

        [Test]
        public void WorldBounds_DistanceToEdgeIsZeroOnBorder()
        {
            Rect bounds = Constants.WorldBounds;

            Assert.That(WorldBounds.DistanceToEdge(new Vector2(bounds.xMin, bounds.center.y)), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(WorldBounds.DistanceToEdge(bounds.center), Is.GreaterThan(100f));
        }

        [TestCase(SessionFailureReasons.RoomFull, true, SessionFailure.RoomFull)]
        [TestCase(SessionFailureReasons.VersionMismatch, true, SessionFailure.VersionMismatch)]
        [TestCase("", true, SessionFailure.HostLeft)]
        [TestCase("", false, SessionFailure.ConnectionFailed)]
        [TestCase("Timeout", true, SessionFailure.ConnectionFailed)]
        public void SessionFailure_IsDerivedFromDisconnectReason(string reason, bool wasClient, SessionFailure expected)
        {
            Assert.That(SessionFailureReasons.FromDisconnectReason(reason, wasClient), Is.EqualTo(expected));
        }

        [Test]
        public void BotNames_AreStableAndNotEmpty()
        {
            for (ulong seed = 0; seed < 100; seed++)
            {
                Assert.That(BotNames.Get(seed), Is.Not.Empty);
                Assert.That(BotNames.Get(seed), Is.EqualTo(BotNames.Get(seed)));
            }
        }

        [TestCase("Nova", true)]
        [TestCase("Маша", true)]
        [TestCase("Sh1t_Lord", false)]
        [TestCase("f.u.c.k", false)]
        [TestCase("СуКа", false)]
        public void NicknameFilter_BlocksObviousProfanity(string nickname, bool allowed)
        {
            Assert.That(NicknameFilter.IsAllowed(nickname), Is.EqualTo(allowed));
        }

        [Test]
        public void NicknameRules_ReplaceBlockedNickname()
        {
            Assert.That(NicknameRules.Normalize("fuck you"), Is.EqualTo(NicknameRules.DefaultNickname));
        }

        [Test]
        public void BotTuning_HunterIsBolderThanCautious()
        {
            BotTuning hunter = BotTuning.For(BotPersonality.Hunter);
            BotTuning cautious = BotTuning.For(BotPersonality.Cautious);

            Assert.That(hunter.Awareness, Is.GreaterThan(cautious.Awareness));
            Assert.That(hunter.HuntRatio, Is.LessThan(cautious.HuntRatio));
            Assert.That(hunter.ThreatRatio, Is.GreaterThan(cautious.ThreatRatio));
        }

        [Test]
        public void BotTuning_PersonalitiesAreDistributed()
        {
            int[] counts = new int[3];
            for (ulong id = 5; id < 5 + 3 * 30; id += 3)
            {
                counts[(int)BotTuning.PersonalityFor(id)]++;
            }

            Assert.That(counts, Has.All.GreaterThanOrEqualTo(5), string.Join(",", counts));
        }

        [Test]
        public void EnemyTurnRules_TurnsSmoothlyWithoutDash()
        {
            Assert.That(EnemyDecisionRules.GetTurnSpeed(260f, 900f, 0f), Is.EqualTo(260f));
        }

        [Test]
        public void EnemyTurnRules_DashesWhileBurstIsActive()
        {
            Assert.That(EnemyDecisionRules.GetTurnSpeed(260f, 900f, 0.3f), Is.EqualTo(900f));
        }

        [Test]
        public void EnemyTurnRules_NeverTurnsSlowerThanSmoothSpeed()
        {
            Assert.That(EnemyDecisionRules.GetTurnSpeed(260f, 100f, 0.3f), Is.EqualTo(260f));
            Assert.That(EnemyDecisionRules.GetTurnSpeed(-50f, 900f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void SceneContainers_GroupObjectsUnderRuntimeRoot()
        {
            GameObject item = new("ContainerTestItem");
            try
            {
                SceneContainers.Attach(item.transform, SceneContainers.Effects);

                Assert.That(item.transform.parent.name, Is.EqualTo(SceneContainers.Effects));
                Assert.That(item.transform.parent.parent.name, Is.EqualTo("[Runtime]"));
                Assert.That(item.scene, Is.EqualTo(SceneManager.GetActiveScene()));
            }
            finally
            {
                Object.DestroyImmediate(item.transform.root.gameObject);
            }
        }
    }
}
