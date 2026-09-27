using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class ReleaseReadinessTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy.prefab";
        private const string CometPrefabPath = "Assets/_Project/Prefabs/Comet.prefab";
        private const string AdMobSettingsPath = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string AdMobSampleAppId = "ca-app-pub-3940256099942544~3347511713";

        [Test]
        [Category("Slow")]
        [Timeout(600000)]
        public void PlayerScripts_CompileForAndroid()
        {
            Assume.That(
                BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android),
                Is.True,
                "Android Build Support is not installed.");

            string outputDirectory = Path.Combine("Temp", "PlanetIO_AndroidScriptCheck");
            ScriptCompilationSettings settings = new()
            {
                group = BuildTargetGroup.Android,
                target = BuildTarget.Android,
                options = ScriptCompilationOptions.None
            };

            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, outputDirectory);

            Assert.That(result.assemblies, Is.Not.Empty, "Android player scripts failed to compile.");
            Assert.That(result.assemblies, Does.Contain("PlanetIO.Infrastructure.dll"));
        }

        [Test]
        public void BuildSettings_StartWithBootAndReferenceExistingScenes()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

            Assert.That(scenes, Is.Not.Empty);
            Assert.That(Path.GetFileNameWithoutExtension(scenes[0].path), Is.EqualTo(SceneNames.Boot));
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                Assert.That(File.Exists(scene.path), Is.True, $"Missing build scene: {scene.path}");
            }
        }

        [Test]
        public void AdMob_AndroidAppIdIsConfigured()
        {
            Object settings = AssetDatabase.LoadMainAssetAtPath(AdMobSettingsPath);
            Assert.That(settings, Is.Not.Null);

            string appId = new SerializedObject(settings).FindProperty("adMobAndroidAppId").stringValue;

            Assert.That(appId, Does.StartWith("ca-app-pub-"));
            Assert.That(appId, Is.Not.EqualTo(AdMobSampleAppId));
        }

        [TestCase("SetBoostRpc")]
        [TestCase("SetColorRpc")]
        [TestCase("SubmitNicknameRpc")]
        [TestCase("ContinueRpc")]
        [TestCase("RespawnRpc")]
        public void PlayerServerRpc_OnlyAcceptsCallsFromOwner(string methodName)
        {
            MethodInfo method = typeof(Player).GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);

            RpcAttribute rpc = method.GetCustomAttribute<RpcAttribute>();
            Assert.That(rpc, Is.Not.Null, methodName);
            Assert.That(rpc.InvokePermission, Is.EqualTo(RpcInvokePermission.Owner));
        }

        [TestCase(PlayerPrefabPath)]
        [TestCase(EnemyPrefabPath)]
        public void PlanetPrefab_CapacityBoundsAreConsistent(string prefabPath)
        {
            SerializedObject planet = LoadComponent<PlanetScale>(prefabPath);

            float min = planet.FindProperty("_minCapacity").floatValue;
            float max = planet.FindProperty("_maxCapacity").floatValue;
            float initial = planet.FindProperty("_initialCapacity").floatValue;
            float eatRatio = planet.FindProperty("_eatSizeRatio").floatValue;

            Assert.That(min, Is.GreaterThan(0f));
            Assert.That(initial, Is.GreaterThan(min), "Spawning at MinCapacity kills on the first hit.");
            Assert.That(max, Is.GreaterThan(initial));
            Assert.That(eatRatio, Is.GreaterThan(1f), "Equal-sized planets must not eat each other.");
        }

        [Test]
        public void PlayerPrefab_BoostIsFasterThanCruise()
        {
            SerializedObject movement = LoadComponent<PlayerMovement>(PlayerPrefabPath);

            Assert.That(
                movement.FindProperty("_boostSpeed").floatValue,
                Is.GreaterThan(movement.FindProperty("_normalSpeed").floatValue));
        }

        [Test]
        public void PlayerPrefab_ContinueRestoresMoreThanMinimum()
        {
            SerializedObject player = LoadComponent<Player>(PlayerPrefabPath);

            Assert.That(player.FindProperty("_continueMassFraction").floatValue, Is.InRange(0.01f, 1f));
            Assert.That(player.FindProperty("_continueProtectionTime").floatValue, Is.GreaterThan(0f));
        }

        [Test]
        public void CometPrefab_ActuallyDrifts()
        {
            SerializedObject comet = LoadComponent<CometMovement>(CometPrefabPath);

            Assert.That(
                comet.FindProperty("_minimumSpeed").floatValue,
                Is.GreaterThanOrEqualTo(0.1f),
                "Comets slower than 0.1 u/s look frozen on a 440-unit map.");
        }

        private static SerializedObject LoadComponent<T>(string prefabPath) where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);

            T component = prefab.GetComponent<T>();
            Assert.That(component, Is.Not.Null, $"{typeof(T).Name} on {prefabPath}");
            return new SerializedObject(component);
        }
    }
}
