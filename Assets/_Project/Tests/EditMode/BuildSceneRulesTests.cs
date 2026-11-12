using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace PlanetIO.Tests
{
    public sealed class BuildSceneRulesTests
    {
        private const string AudioListenerDocumentPrefix = "--- !u!81 ";

        [Test]
        public void BuildScenes_EachHaveExactlyOneAudioListener()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            Assert.That(scenes, Is.Not.Empty);

            foreach (EditorBuildSettingsScene scene in scenes)
            {
                int listeners = File.ReadLines(scene.path)
                    .Count(line => line.StartsWith(AudioListenerDocumentPrefix));

                Assert.That(listeners, Is.EqualTo(1), scene.path);
            }
        }
    }
}
