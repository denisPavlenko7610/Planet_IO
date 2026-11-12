using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class NumericEdgeCaseTests
    {
        private const string EnemyPrefabPath = "Assets/_Project/Prefabs/Enemy.prefab";

        private Enemy _planet;

        [SetUp]
        public void SetUp()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            Assert.That(prefab, Is.Not.Null, EnemyPrefabPath);
            _planet = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<Enemy>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_planet.gameObject);
        }

        [Test]
        public void Capacity_NaNIsIgnored()
        {
            _planet.Capacity = 0.5f;

            _planet.Capacity = float.NaN;

            Assert.That(_planet.Capacity, Is.EqualTo(0.5f));
        }

        [Test]
        public void Capacity_InfinityClampsToConfiguredBounds()
        {
            _planet.Capacity = float.PositiveInfinity;
            Assert.That(_planet.Capacity, Is.EqualTo(MaxCapacity()));

            _planet.Capacity = float.NegativeInfinity;
            Assert.That(_planet.Capacity, Is.EqualTo(_planet.MinCapacity));
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void Capacity_ZeroAndNegativeClampToMinimum(float value)
        {
            _planet.Capacity = 0.5f;

            _planet.Capacity = value;

            Assert.That(_planet.Capacity, Is.EqualTo(_planet.MinCapacity));
        }

        [Test]
        public void Capacity_ScaleFollowsClampedValue()
        {
            _planet.Capacity = MaxCapacity() * 100f;

            Assert.That(_planet.transform.localScale.x, Is.EqualTo(MaxCapacity()).Within(1e-6f));
            Assert.That(_planet.transform.localScale.y, Is.EqualTo(MaxCapacity()).Within(1e-6f));
        }

        [TestCase(0.01f, 0f, 1f)]
        [TestCase(0.01f, -1f, 1f)]
        [TestCase(0.01f, 1.5f, 0f)]
        [TestCase(0.01f, 1.5f, -2f)]
        public void GrowthFalloff_NonPositiveCapacityOrFalloffLeavesGrowthUntouched(
            float growth,
            float capacity,
            float falloff)
        {
            Assert.That(GrowthRules.ApplyFalloff(growth, capacity, falloff, false), Is.EqualTo(growth));
        }

        [Test]
        public void GrowthFalloff_InfiniteCapacityGivesZeroInsteadOfNaN()
        {
            float result = GrowthRules.ApplyFalloff(0.01f, float.PositiveInfinity, 1f, false);

            Assert.That(float.IsNaN(result), Is.False);
            Assert.That(result, Is.EqualTo(0f));
        }

        private float MaxCapacity() => new SerializedObject(_planet).FindProperty("_maxCapacity").floatValue;
    }
}
