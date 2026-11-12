using System.Collections.Generic;
using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class PlanetNamesTests
    {
        [Test]
        public void AllNames_AreUniqueAndFitNickname()
        {
            HashSet<string> seen = new();
            for (int index = 0; index < PlanetNames.Count; index++)
            {
                string name = PlanetNames.Get(index);
                Assert.That(name, Is.EqualTo(NicknameRules.Normalize(name)), name);
                Assert.That(name.Length, Is.LessThanOrEqualTo(NicknameRules.MaximumLength), name);
                Assert.That(NicknameFilter.IsAllowed(name), Is.True, name);
                Assert.That(seen.Add(name), Is.True, $"Duplicate: {name}");
            }
        }

        [Test]
        public void Pick_NeverReturnsExcludedName()
        {
            System.Random random = new(1);
            string excluded = PlanetNames.Get(0);
            for (int attempt = 0; attempt < 500; attempt++)
            {
                Assert.That(PlanetNames.Pick(random, excluded), Is.Not.EqualTo(excluded));
            }
        }
    }
}
