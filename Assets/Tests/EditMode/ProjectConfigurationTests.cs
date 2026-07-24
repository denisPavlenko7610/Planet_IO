using System.Linq;
using NUnit.Framework;
using Planet_IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace PlanetIO.Tests
{
    public sealed class ProjectConfigurationTests
    {
        private static readonly string[] StreamingMusicPaths =
        {
            "Assets/Audio/BossMain.wav",
            "Assets/Audio/Map (basic version).wav",
            "Assets/Audio/Map.wav",
            "Assets/Audio/Mars.wav",
            "Assets/Audio/Mercury.wav",
            "Assets/Audio/Venus.wav"
        };

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

        [Test]
        public void AndroidRelease_UsesApi36Il2CppAndArm64()
        {
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Android),
                Is.EqualTo("com.rd.planetio"));
            Assert.That(
                PlayerSettings.Android.targetSdkVersion,
                Is.EqualTo(AndroidSdkVersions.AndroidApiLevel36));
            Assert.That(
                PlayerSettings.GetScriptingBackend(
                    NamedBuildTarget.Android),
                Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(
                PlayerSettings.Android.targetArchitectures.HasFlag(
                    AndroidArchitecture.ARM64),
                Is.True);
            Assert.That(
                PlayerSettings.allowedAutorotateToPortrait,
                Is.False);
            Assert.That(
                PlayerSettings.allowedAutorotateToPortraitUpsideDown,
                Is.False);
        }

        [Test]
        public void LongMusicTracks_AreStreamedInBackground()
        {
            foreach (string assetPath in StreamingMusicPaths)
            {
                AudioImporter importer =
                    AssetImporter.GetAtPath(assetPath) as AudioImporter;

                Assert.That(
                    importer,
                    Is.Not.Null,
                    $"Audio importer is missing for {assetPath}.");
                Assert.That(
                    importer.defaultSampleSettings.loadType,
                    Is.EqualTo(AudioClipLoadType.Streaming),
                    assetPath);
                Assert.That(
                    importer.defaultSampleSettings.preloadAudioData,
                    Is.False,
                    assetPath);
                Assert.That(
                    importer.loadInBackground,
                    Is.True,
                    assetPath);
            }
        }

        [Test]
        public void MobileControlTexture_IsLimitedTo512Pixels()
        {
            const string assetPath =
                "Assets/Sprites/Buttons/AccelerationButton/TriangleImg.png";
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(
                importer.maxTextureSize,
                Is.LessThanOrEqualTo(512));
        }

        [TestCase("Assets/Atlases/Gameplay.spriteatlas", 2048)]
        [TestCase("Assets/Atlases/Controls.spriteatlas", 1024)]
        public void SpriteAtlas_IsPackedForAndroid(
            string assetPath,
            int maximumTextureSize)
        {
            SpriteAtlas atlas =
                AssetDatabase.LoadAssetAtPath<SpriteAtlas>(assetPath);

            Assert.That(atlas, Is.Not.Null);
            Assert.That(atlas.GetPackables(), Is.Not.Empty);

            TextureImporterPlatformSettings androidSettings =
                atlas.GetPlatformSettings("Android");
            Assert.That(androidSettings.overridden, Is.True);
            Assert.That(
                androidSettings.maxTextureSize,
                Is.EqualTo(maximumTextureSize));
            Assert.That(
                androidSettings.format,
                Is.EqualTo(TextureImporterFormat.ETC2_RGBA8));
        }
    }
}
