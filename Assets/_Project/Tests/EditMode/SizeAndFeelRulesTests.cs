using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class SizeAndFeelRulesTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy.prefab";

		[TestCase(30)]
		[TestCase(60)]
		[TestCase(120)]
		public void Growth_SettlesSmoothlyWithoutOvershootAtDifferentFrameRates(int fps)
		{
			float start = PlanetSizeRules.Scale(0.08f);
			float target = PlanetSizeRules.Scale(0.8f);
			float scale = start;
			for (int i = 0; i < fps; i++)
			{
				float next = PlanetSizeRules.SmoothGrowth(scale, target, 1f / fps);
				Assert.That(next, Is.InRange(scale, target));
				if (i == 0) Assert.That(next, Is.LessThan(start + (target - start) * 0.21f));
				scale = next;
			}
			Assert.That(scale, Is.EqualTo(target).Within((target - start) * 0.002f));
			Assert.That(PlanetSizeRules.SmoothGrowth(scale, start, 0.01f), Is.EqualTo(start));
		}

		[Test]
		public void Steering_ReversalIsGradualAndConvergesWithoutOvershoot()
		{
			float angle = 0f;
			float velocity = 0f;
			for (int i = 0; i < 100; i++)
			{
				float next = MovementRules.Turn(angle, 170f, ref velocity,
					MovementRules.TurnSmoothTime, MovementRules.TurnSpeed, 0.02f);
				Assert.That(next, Is.InRange(angle, 170f));
				Assert.That(next - angle, Is.LessThanOrEqualTo(MovementRules.TurnSpeed * 0.02f));
				angle = next;
			}
			Assert.That(angle, Is.EqualTo(170f).Within(0.1f));
		}

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

        [TestCase(0.16f, 1.3195f)]
        [TestCase(0.35f, 1.8046f)]
        [TestCase(0.8f, 2.5119f)]
        [TestCase(2f, 3.6239f)]
        public void CameraGrowth_SettledDiameterRemainsReadable(float mass, float expected)
        {
            float body = PlanetSizeRules.Scale(mass) / PlanetSizeRules.Scale(0.08f);
            float zoom = CameraZoomRules.Ratio(mass, CameraZoomRules.GrowthExponent);
            Assert.That(body / zoom, Is.EqualTo(expected).Within(0.002f));
        }

        [Test]
        public void CameraGrowth_ContinuousFeedingDoesNotRestartHold()
        {
            GrowthZoomState zoom = new();
            zoom.Reset(3.6f);
            Assert.That(zoom.Step(3.8f, 2f, 0.1f), Is.EqualTo(3.6f));
            Assert.That(zoom.Step(4f, 2f, 0.1f), Is.EqualTo(3.6f));
            Assert.That(zoom.Step(4.2f, 2f, 0.1f), Is.EqualTo(3.6f));
            Assert.That(zoom.Step(4.4f, 2f, 0.1f), Is.EqualTo(4.4f));
        }

        [Test]
        public void CameraGrowth_ShrinkAndMoonSafetyBypassHold()
        {
            GrowthZoomState zoom = new();
            zoom.Reset(3.6f);
            Assert.That(zoom.Step(5f, 4.5f, 0.01f), Is.EqualTo(4.5f));
            Assert.That(zoom.Step(3.6f, 2f, 0.01f), Is.EqualTo(3.6f));
            zoom.Reset(3.6f);
            Assert.That(zoom.Step(3.8f, 2f, 0.01f), Is.EqualTo(3.6f));
        }

        [TestCase(0.5625f)]
        [TestCase(1f)]
        [TestCase(1.7778f)]
        public void CameraMoons_FitBothAxesWithLookAhead(float aspect)
        {
            float view = CameraZoomRules.MoonView(2f, 1.45f, aspect, 0.16f);
            float offset = view * 0.16f;
            Assert.That(view - offset, Is.GreaterThanOrEqualTo(2f));
            Assert.That(view * aspect - offset, Is.GreaterThanOrEqualTo(2f));
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
            var world = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = world.Create(Vector2.zero, 0.2f);
            try
            {
                food.SetDropper(7UL, 5f);

                Assert.That(food.IsMagnetImmuneFor(7UL), Is.True);
                Assert.That(food.IsMagnetImmuneFor(8UL), Is.False);

                world.Renew(food);
                Assert.That(food.IsMagnetImmuneFor(7UL), Is.False);
            }
            finally
            {
                world.Remove(food);
            }
        }

        [TestCase(PlayerPrefabPath)]
        [TestCase(EnemyPrefabPath)]
        public void Growth_MaximumMassAllowsTwiceThePreviousMaximumDiameter(string prefabPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            SerializedObject planet = new(prefab.GetComponent<PlanetScale>());
            float maximum = planet.FindProperty("_maxCapacity").floatValue;
            Assert.That(maximum, Is.EqualTo(PlanetSizeRules.MaximumCapacity));
            Assert.That(PlanetSizeRules.Scale(maximum) / PlanetSizeRules.Scale(2f), Is.EqualTo(2f).Within(1e-5f));
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
