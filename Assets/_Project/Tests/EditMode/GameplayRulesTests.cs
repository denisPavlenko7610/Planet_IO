using NUnit.Framework;
using PlanetIO;
using PlanetIO.Application;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class GameplayRulesTests
    {
        private sealed class FoodTestScope : System.IDisposable
        {
            public readonly Food Food;

            public FoodTestScope()
            {
                Food = new GameObject("TestFood").AddComponent<Food>();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Food.gameObject);
            }
        }

        [Test]
        public void Food_ClaimIsExclusiveUntilReset()
        {
            using var scope = new FoodTestScope();
            Assert.That(scope.Food.TryClaim(), Is.True);
            Assert.That(scope.Food.TryClaim(), Is.False);

            scope.Food.ResetClaim();

            Assert.That(scope.Food.TryClaim(), Is.True);
        }

        [Test]
        public void Food_LifecycleTracksDroppedStateAndVersion()
        {
            using var scope = new FoodTestScope();
            Assert.That(scope.Food.IsDropped, Is.False);
            Assert.That(scope.Food.LifecycleVersion, Is.EqualTo(0));

            int version = scope.Food.MarkAsDropped();

            Assert.That(scope.Food.IsDropped, Is.True);
            Assert.That(version, Is.EqualTo(1));
            Assert.That(scope.Food.MarkAsDropped(), Is.EqualTo(2));

            scope.Food.MarkAsStored();

            Assert.That(scope.Food.IsDropped, Is.False);
            Assert.That(scope.Food.LifecycleVersion, Is.EqualTo(3));
        }

        [Test]
        public void Food_DefaultValueMultiplierIsOne_AndStaysWithoutServer()
        {
            using var scope = new FoodTestScope();
            Assert.That(scope.Food.ValueMultiplier, Is.EqualTo(1f));

            scope.Food.SetValueMultiplier(8f);

            Assert.That(scope.Food.ValueMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void PlayerPalette_GetColorWrapsAroundBounds()
        {
            int count = PlayerPalette.Colors.Count;

            Assert.That(PlayerPalette.GetColor(count), Is.EqualTo(PlayerPalette.Colors[0]));
            Assert.That(PlayerPalette.GetColor(-1), Is.EqualTo(PlayerPalette.Colors[1]));
        }

        [Test]
        public void PlayerPalette_IndexOfFindsPaletteColorOnly()
        {
            Color32 known = PlayerPalette.Colors[2];
            Color32 unknown = new(9, 17, 33, 255);

            Assert.That(PlayerPalette.IndexOf(known), Is.EqualTo(2));
            Assert.That(PlayerPalette.IndexOf(unknown), Is.EqualTo(-1));
        }

        [Test]
        public void NicknameRules_NormalizeTrimsAndCollapsesSpaces()
        {
            Assert.That(NicknameRules.Normalize("  Bob   Marley "), Is.EqualTo("Bob Marley"));
        }

        [Test]
        public void NicknameRules_NormalizeRemovesControlCharacters()
        {
            Assert.That(NicknameRules.Normalize("bo\rb\ny"), Is.EqualTo("boby"));
        }

        [Test]
        public void NicknameRules_NormalizeClampsLength()
        {
            string longNickname = new('a', NicknameRules.MaximumLength + 10);

            Assert.That(
                NicknameRules.Normalize(longNickname).Length,
                Is.EqualTo(NicknameRules.MaximumLength));
        }

        [TestCase(null)]
        [TestCase("   ")]
        public void NicknameRules_EmptyBecomesDefault(string nickname)
        {
            Assert.That(NicknameRules.Normalize(nickname), Is.EqualTo(NicknameRules.DefaultNickname));
        }

        [Test]
        public void RoomRules_NormalizeRoomCodeUppersAndStrips()
        {
            Assert.That(RoomRules.NormalizeRoomCode(" ab-12cd "), Is.EqualTo("AB12CD"));
        }

        [Test]
        public void RoomRules_NormalizeRoomCodeClampsLength()
        {
            string longCode = new('a', RoomRules.MaximumRoomCodeLength + 10);

            Assert.That(RoomRules.NormalizeRoomCode(longCode).Length, Is.EqualTo(RoomRules.MaximumRoomCodeLength));
        }

        [TestCase("abc", false)]
        [TestCase("ABCD", true)]
        public void RoomRules_ValidatesMinimumLength(string roomCode, bool expected)
        {
            Assert.That(RoomRules.IsValidRoomCode(roomCode), Is.EqualTo(expected));
        }

        [TestCase(-5, 1)]
        [TestCase(0, 1)]
        [TestCase(7, 7)]
        [TestCase(100, 16)]
        public void RoomRules_ClampsMaxPlayers(int input, int expected)
        {
            Assert.That(RoomRules.ClampMaxPlayers(input), Is.EqualTo(expected));
        }

        [Test]
        public void RoomRules_TryCreateConnectionSettingsRejectsShortCode()
        {
            Assert.That(
                RoomRules.TryCreateConnectionSettings("ab", out _, out string error),
                Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void RoomRules_TryCreateConnectionSettingsNormalizesCode()
        {
            bool created = RoomRules.TryCreateConnectionSettings(" planet ", out RoomConnectionSettings settings, out _);

            Assert.That(created, Is.True);
            Assert.That(settings.RoomCode, Is.EqualTo("PLANET"));
        }

        [Test]
        public void RoomRules_CreateRoomCodeIsShortAlphanumeric()
        {
            string code = RoomRules.CreateRoomCode();

            Assert.That(code.Length, Is.EqualTo(6));
            Assert.That(code, Is.EqualTo(RoomRules.NormalizeRoomCode(code)));
        }

        [Test]
        public void Constants_ClampToWorldKeepsInsidePoint()
        {
            Vector3 clamped = Constants.ClampToWorld(new Vector3(10f, 20f, 5f));

            Assert.That(clamped, Is.EqualTo(new Vector3(10f, 20f, 5f)));
        }

        [Test]
        public void Constants_ClampToWorldPullsOutsidePointToBorder()
        {
            Vector3 clamped = Constants.ClampToWorld(new Vector3(-300f, 300f, 5f));

            Assert.That(clamped.x, Is.EqualTo(Constants.WorldBounds.xMin));
            Assert.That(clamped.y, Is.EqualTo(Constants.WorldBounds.yMax));
        }

        [Test]
        public void EnemyDecisionRules_HazardAlwaysEvades()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f, 0f, hasVisibleFood: true, hasImmediateHazard: true, 1.1f, 1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Evade));
        }

        [Test]
        public void EnemyDecisionRules_BiggerPlayerCausesEvade()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f, 2f, hasVisibleFood: true, hasImmediateHazard: false, 1.1f, 1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Evade));
        }

        [Test]
        public void EnemyDecisionRules_SmallerPlayerCausesHunt()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1.2f, 1f, hasVisibleFood: true, hasImmediateHazard: false, 1.1f, 1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Hunt));
        }

        [Test]
        public void EnemyDecisionRules_SimilarPlayerForagesFood()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f, 0.95f, hasVisibleFood: true, hasImmediateHazard: false, 1.1f, 1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Forage));
        }

        [Test]
        public void EnemyDecisionRules_SimilarPlayerWithoutFoodRoams()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f, 0.95f, hasVisibleFood: false, hasImmediateHazard: false, 1.1f, 1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Roam));
        }

        [Test]
        public void EnemyDecisionRules_MinimumCapacityKeepsFullSpeed()
        {
            float multiplier = EnemyDecisionRules.GetCapacitySpeedMultiplier(0.1f, 0.1f, 0.35f, 0.55f);

            Assert.That(multiplier, Is.EqualTo(1f));
        }

        [Test]
        public void EnemyDecisionRules_HugeCapacityClampsToMinimumMultiplier()
        {
            float multiplier = EnemyDecisionRules.GetCapacitySpeedMultiplier(5f, 0.1f, 0.35f, 0.55f);

            Assert.That(multiplier, Is.EqualTo(0.55f));
        }

        [Test]
        public void EnemyDecisionRules_SpeedMultiplierNeverGrowsWithCapacity()
        {
            float smallMultiplier = EnemyDecisionRules.GetCapacitySpeedMultiplier(0.5f, 0.1f, 0.35f, 0.55f);
            float bigMultiplier = EnemyDecisionRules.GetCapacitySpeedMultiplier(0.8f, 0.1f, 0.35f, 0.55f);

            Assert.That(bigMultiplier, Is.LessThanOrEqualTo(smallMultiplier));
        }

        [Test]
        public void NetworkWorldReadyState_MarkReadyWithoutServerKeepsWaiting()
        {
            NetworkWorldReadyState state =
                new GameObject("WorldReadyState").AddComponent<NetworkWorldReadyState>();

            try
            {
                state.MarkReady();

                Assert.That(state.IsReady, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(state.gameObject);
            }
        }
    }
}
