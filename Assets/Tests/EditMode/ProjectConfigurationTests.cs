using System.Linq;
using NUnit.Framework;
using Planet_IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class ProjectConfigurationTests
    {
        [Test]
        public void EnemyPool_AlwaysProvidesAtLeastTenOpponents()
        {
            GameObject gameObject = new("EnemyPoolTest");
            try
            {
                EnemyPool pool = gameObject.AddComponent<EnemyPool>();
                SerializedObject serializedPool = new(pool);
                serializedPool.FindProperty("_capacity").intValue = 1;
                serializedPool.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(
                    pool.Capacity,
                    Is.GreaterThanOrEqualTo(
                        EnemyPool.MinimumOpponentCount));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Addressables_AudioGroupIsConfiguredForLocalPreload()
        {
            AddressableAssetSettings settings =
                AddressableAssetSettingsDefaultObject.Settings;
            Assert.That(settings, Is.Not.Null);

            AddressableAssetGroup group =
                settings.FindGroup("PlanetIO_Audio");
            Assert.That(group, Is.Not.Null);
            Assert.That(
                group.GetSchema<BundledAssetGroupSchema>(),
                Is.Not.Null);
            Assert.That(
                group.GetSchema<ContentUpdateGroupSchema>(),
                Is.Not.Null);
            Assert.That(group.entries, Is.Not.Empty);
            Assert.That(
                group.entries.All(
                    entry => entry.labels.Contains("audio") &&
                             entry.labels.Contains("preload")),
                Is.True);
        }
    }
}
