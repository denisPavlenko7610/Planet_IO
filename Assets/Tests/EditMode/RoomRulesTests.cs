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
                " 192.168.0.4 ",
                0,
                999);

            Assert.That(settings.RoomCode, Is.EqualTo("ROOM1"));
            Assert.That(settings.Address, Is.EqualTo("192.168.0.4"));
            Assert.That(settings.Port, Is.EqualTo(RoomRules.DefaultPort));
            Assert.That(
                settings.MaxPlayers,
                Is.EqualTo(RoomRules.MaximumPlayers));
        }

        [TestCase("7777", true, 7777)]
        [TestCase("0", false, RoomRules.DefaultPort)]
        [TestCase("not-a-port", false, RoomRules.DefaultPort)]
        public void TryParsePort_ValidatesRange(
            string input,
            bool expectedResult,
            int expectedPort)
        {
            bool result = RoomRules.TryParsePort(input, out ushort port);

            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(port, Is.EqualTo((ushort)expectedPort));
        }
    }
}
