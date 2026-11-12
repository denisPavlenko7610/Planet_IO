using System.Collections.Generic;
using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class CometNamesTests
    {
        [Test]
        public void AllNames_AreUniqueAndShort()
        {
            HashSet<string> seen = new();
            for (int index = 0; index < CometNames.Count; index++)
            {
                string name = CometNames.Get(index);
                Assert.That(name, Is.Not.Empty);
                Assert.That(name.Length, Is.LessThanOrEqualTo(NicknameRules.MaximumLength), name);
                Assert.That(seen.Add(name), Is.True, $"Duplicate: {name}");
            }
        }

        [Test]
        public void Get_IsStableForSameSeed()
        {
            Assert.That(CometNames.Get(12345UL), Is.EqualTo(CometNames.Get(12345UL)));
            Assert.That(CometNames.Get(0UL), Is.EqualTo(CometNames.Get(0)));
        }

        [Test]
        public void ShowCometNames_IsOffByDefault()
        {
            Assert.That(GameSettingKeys.ShowCometNames.DefaultValue, Is.False);
        }
    }
}
