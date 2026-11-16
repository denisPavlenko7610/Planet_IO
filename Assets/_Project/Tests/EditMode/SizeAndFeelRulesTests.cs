using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class SizeAndFeelRulesTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy.prefab";

        [Test]
        public void PlanetScale_AtReferenceCapacity_EqualsCapacity()
        {
            Assert.That(PlanetSizeRules.Scale(PlanetSizeRules.ReferenceCapacity),
                Is.EqualTo(PlanetSizeRules.ReferenceCapacity).Within(1e-6f));
        }

        [Test]
        public void PlanetScale_GrowsWithAreaSoSizeRangeStaysNarrow()
        {
            float smallest = PlanetSizeRules.Scale(0.08f);
            float largest = PlanetSizeRules.Scale(2f);

            Assert.That(largest, Is.GreaterThan(smallest));
            Assert.That(largest / smallest, Is.EqualTo(5f).Within(1e-3f));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void PlanetScale_NonPositiveCapacity_StaysPositive(float capacity)
        {
            Assert.That(PlanetSizeRules.Scale(capacity), Is.GreaterThan(0f));
        }

        [Test]
        public void CameraZoom_NeverZoomsInBelowBase_AndGrowsSlowerThanPlanet()
        {
            Assert.That(CameraZoomRules.Ratio(0.01f, 0.28f), Is.EqualTo(1f));

            float zoom = CameraZoomRules.Ratio(2f, 0.28f);
            float planet = PlanetSizeRules.Scale(2f) / PlanetSizeRules.Scale(PlanetSizeRules.ReferenceCapacity);

            Assert.That(zoom, Is.GreaterThan(1f));
            Assert.That(zoom, Is.LessThan(planet));
        }

        [Test]
        public void FoodViewScale_IsBoostedAtBaseZoomAndCapped()
        {
            Assert.That(FoodViewScale.Resolve(1f), Is.EqualTo(FoodViewScale.BaseBoost).Within(1e-5f));
            Assert.That(FoodViewScale.Resolve(1000f), Is.EqualTo(FoodViewScale.MaximumFactor).Within(1e-5f));
            Assert.That(FoodViewScale.Resolve(2f), Is.GreaterThan(FoodViewScale.Resolve(1f)));
        }

        [Test]
        public void FoodViewScale_RaisesChangedOnlyWhenQuantizedFactorMoves()
        {
            FoodViewScale.SetZoomRatio(1f);
            int raised = 0;
            void Handler(float _) => raised++;
            FoodViewScale.Changed += Handler;

            try
            {
                FoodViewScale.SetZoomRatio(1f);
                FoodViewScale.SetZoomRatio(1.001f);
                Assert.That(raised, Is.Zero);

                FoodViewScale.SetZoomRatio(2.4f);
                Assert.That(raised, Is.EqualTo(1));
                Assert.That(FoodViewScale.Factor, Is.GreaterThan(FoodViewScale.BaseBoost));
            }
            finally
            {
                FoodViewScale.Changed -= Handler;
                FoodViewScale.SetZoomRatio(1f);
            }
        }

        [TestCase(0.28f)]
        [TestCase(1f)]
        [TestCase(1.42f)]
        public void FoodMagnet_ReachAlwaysExceedsBody(float bodyRadius)
        {
            float reach = FoodMagnetRules.Reach(bodyRadius, 0.2f, 1f);

            Assert.That(reach, Is.GreaterThan(bodyRadius));
        }

        [Test]
        public void FoodMagnet_ReachGrowsWithBodyAndPerk()
        {
            Assert.That(
                FoodMagnetRules.Reach(1.4f, 0.2f, 1f) - 1.4f,
                Is.GreaterThan(FoodMagnetRules.Reach(0.28f, 0.2f, 1f) - 0.28f));
            Assert.That(
                FoodMagnetRules.Reach(1f, 0.2f, 1.5f),
                Is.GreaterThan(FoodMagnetRules.Reach(1f, 0.2f, 1f)));
        }

        [Test]
        public void Food_MagnetImmunityAppliesOnlyToDropperDuringGrace()
        {
            var food = new GameObject("ImmuneFood").AddComponent<Food>();
            try
            {
                food.SetDropper(7UL, 5f);

                Assert.That(food.IsMagnetImmuneFor(7UL), Is.True);
                Assert.That(food.IsMagnetImmuneFor(8UL), Is.False);

                food.MarkAsStored();
                Assert.That(food.IsMagnetImmuneFor(7UL), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(food.gameObject);
            }
        }

        [TestCase(PlayerPrefabPath)]
        [TestCase(EnemyPrefabPath)]
        public void Speed_BigPlanetsAreNeverFasterThanSmallOnes(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, prefabPath);

            SerializedObject planet = new(prefab.GetComponent<PlanetScale>());
            float minCapacity = planet.FindProperty("_minCapacity").floatValue;
            float maxCapacity = planet.FindProperty("_maxCapacity").floatValue;

            Component movement = prefabPath == PlayerPrefabPath
                ? prefab.GetComponent<PlayerMovement>()
                : prefab.GetComponent<EnemyMovement>();
            SerializedObject tuning = new(movement);
            float penalty = tuning.FindProperty("_massSpeedPenalty").floatValue;
            float floor = tuning.FindProperty("_minimumSpeedMultiplier").floatValue;

            float small = EnemyDecisionRules.GetCapacitySpeedMultiplier(minCapacity, minCapacity, penalty, floor);
            float large = EnemyDecisionRules.GetCapacitySpeedMultiplier(maxCapacity, minCapacity, penalty, floor);

            Assert.That(large, Is.LessThanOrEqualTo(small));
            Assert.That(large, Is.EqualTo(small).Within(1e-5f), "Fights are meant to be speed-equal.");
        }
    }
}
