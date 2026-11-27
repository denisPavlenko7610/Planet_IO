using System;
using NUnit.Framework;
using PlanetIO.Infrastructure.Audio;
using UnityEditor;

namespace PlanetIO.Tests
{
    public sealed class SfxCatalogTests
    {
        private const string CatalogPath = "Assets/_Project/Audio/SfxCatalog.asset";

        [Test]
        public void Catalog_HasAnAssignedCueForEverySfxId()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
            Assert.That(catalog != null, Is.True, CatalogPath);

            var serialized = new SerializedObject(catalog);
            SerializedProperty property = serialized.GetIterator();
            int cues = 0;
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.name == "m_Script")
                {
                    continue;
                }

                cues++;
                Assert.That(property.objectReferenceValue != null, Is.True, property.name);
            }

            Assert.That(cues, Is.EqualTo(Enum.GetValues(typeof(SfxId)).Length));
        }

        [TestCase("moon_hit")]
        [TestCase("tier_up")]
        [TestCase("event_warning")]
        public void NewSounds_AreCreditedAsCc0(string file)
        {
            string credits = System.IO.File.ReadAllText("Assets/_Project/Audio/AUDIO_CREDITS.md");

            StringAssert.Contains(file, credits);
            StringAssert.Contains("CC0", credits);
        }
    }
}
