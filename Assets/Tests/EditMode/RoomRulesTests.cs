using NUnit.Framework;
using Planet_IO;

namespace PlanetIO.Tests
{
    public sealed class RoomRulesTests
    {
        [TestCase(" planet-42 ", "PLANET42")]
        [TestCase("a!b@c#d", "ABCD")]
        [TestCase("abcdefghijklmnop", "ABCDEFGHIJKL")]
        public void NormalizeRoomCode_RemovesSeparatorsAndLimitsLength(
            string input,
            string expected)
        {
            Assert.That(
                RoomRules.NormalizeRoomCode(input),
                Is.EqualTo(expected));
        }

        [TestCase("abc", false)]
        [TestCase("ABCD", true)]
        [TestCase("PLANET2026", true)]
        public void IsValidRoomCode_EnforcesLength(
            string input,
            bool expected)
        {
            Assert.That(
                RoomRules.IsValidRoomCode(input),
                Is.EqualTo(expected));
        }

        [Test]
        public void ConnectionSettings_NormalizesAllValues()
        {
            RoomConnectionSettings settings = new(
                " room-1 ",
                999);

            Assert.That(settings.RoomCode, Is.EqualTo("ROOM1"));
            Assert.That(
                settings.MaxPlayers,
                Is.EqualTo(RoomRules.MaximumPlayers));
        }

        [Test]
        public void TryCreateConnectionSettings_NormalizesCompleteForm()
        {
            bool result = RoomRules.TryCreateConnectionSettings(
                " planet-42 ",
                out RoomConnectionSettings settings,
                out string validationError);

            Assert.That(result, Is.True);
            Assert.That(validationError, Is.Empty);
            Assert.That(settings.RoomCode, Is.EqualTo("PLANET42"));
            Assert.That(
                settings.MaxPlayers,
                Is.EqualTo(RoomRules.DefaultMaxPlayers));
        }

        [Test]
        public void TryCreateConnectionSettings_RejectsInvalidForm()
        {
            bool result = RoomRules.TryCreateConnectionSettings(
                "abc",
                out _,
                out string validationError);

            Assert.That(result, Is.False);
            Assert.That(validationError, Is.Not.Empty);
        }

        [Test]
        public void CreateRoomCode_AlwaysCreatesValidCode()
        {
            string roomCode = RoomRules.CreateRoomCode();

            Assert.That(RoomRules.IsValidRoomCode(roomCode), Is.True);
            Assert.That(roomCode, Has.Length.EqualTo(6));
        }
    }
}
