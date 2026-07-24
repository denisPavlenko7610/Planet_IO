using NUnit.Framework;
using Planet_IO;

namespace PlanetIO.Tests
{
    public sealed class NicknameRulesTests
    {
        [TestCase(null, NicknameRules.DefaultNickname)]
        [TestCase("", NicknameRules.DefaultNickname)]
        [TestCase("   ", NicknameRules.DefaultNickname)]
        [TestCase("  Space   Pilot  ", "Space Pilot")]
        [TestCase("Bad\nName\t", "BadName")]
        public void Normalize_ReturnsSafeDisplayName(
            string input,
            string expected)
        {
            Assert.That(NicknameRules.Normalize(input), Is.EqualTo(expected));
        }

        [Test]
        public void Normalize_TruncatesLongNickname()
        {
            string nickname = new('A', NicknameRules.MaximumLength + 5);

            Assert.That(
                NicknameRules.Normalize(nickname),
                Has.Length.EqualTo(NicknameRules.MaximumLength));
        }
    }
}
