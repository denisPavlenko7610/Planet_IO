using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class NicknameFilterExceptionTests
    {
        [TestCase("Dickson")]
        [TestCase("Grape")]
        [TestCase("Drape")]
        [TestCase("Scunthorpe")]
        [TestCase("Matsushita")]
        [TestCase("Mr. Dickens")]
        public void KnownInnocentWords_AreAllowed(string nickname)
        {
            Assert.That(NicknameFilter.IsAllowed(nickname), Is.True);
        }

        [TestCase("Dick")]
        [TestCase("Rape")]
        [TestCase("GrapeRape")]
        [TestCase("shi_grape_t")]
        [TestCase("fuck")]
        [TestCase("Sh1t")]
        public void ProfanityNextToExceptions_StaysBlocked(string nickname)
        {
            Assert.That(NicknameFilter.IsAllowed(nickname), Is.False);
        }
    }
}
