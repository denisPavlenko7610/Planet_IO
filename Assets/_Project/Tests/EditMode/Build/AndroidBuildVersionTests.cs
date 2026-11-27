using NUnit.Framework;
using PlanetIO.Editor;

namespace PlanetIO.Tests
{
    public sealed class AndroidBuildVersionTests
    {
        [TestCase(8, 7, 8)]
        [TestCase(8, 8, 9)]
        [TestCase(5, 8, 9)]
        [TestCase(1, 0, 1)]
        public void Resolve_KeepsAnUnbuiltCodeAndBumpsAnAlreadyBuiltOne(int current, int lastBuilt, int expected)
        {
            Assert.That(AndroidBuildVersion.Resolve(current, lastBuilt), Is.EqualTo(expected));
        }

        [Test]
        public void ResolveKeystorePassword_PrefersTheEnvironmentOverTheStoredValue()
        {
            Assert.That(AndroidSigning.ResolveKeystorePassword("env", "stored"), Is.EqualTo("env"));
            Assert.That(AndroidSigning.ResolveKeystorePassword(null, "stored"), Is.EqualTo("stored"));
            Assert.That(AndroidSigning.ResolveKeystorePassword(string.Empty, null), Is.Empty);
        }
    }
}
