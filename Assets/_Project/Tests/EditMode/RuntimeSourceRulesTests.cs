using System.IO;
using System.Linq;
using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class RuntimeSourceRulesTests
    {
        private const string RuntimeRoot = "Assets/_Project/Runtime";
        private const string RemovedSymbol = "DEVELOPMENT_BUILD";

        [Test]
        public void RuntimeSources_DoNotRelyOnRemovedDevelopmentBuildSymbol()
        {
            string[] offenders = Directory
                .GetFiles(RuntimeRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains(RemovedSymbol))
                .ToArray();

            Assert.That(
                offenders,
                Is.Empty,
                $"{RemovedSymbol} is no longer defined by Unity {UnityEngine.Application.unityVersion}; " +
                "guarded code never compiles, so development builds fall into the release branch. " +
                "Use Debug.isDebugBuild at runtime.");
        }
    }
}
