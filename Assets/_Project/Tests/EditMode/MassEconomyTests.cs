using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class MassEconomyTests
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";

        private Food _food;

        [SetUp]
        public void SetUp()
        {
            _food = new GameObject("TestFood").AddComponent<Food>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_food.gameObject);
        }

        [Test]
        public void Food_WithoutNutrition_GrowsByScaledCapacity()
        {
            _food.Capacity = 0.5f;

            Assert.That(_food.GetGrowth(0.02f), Is.EqualTo(0.01f).Within(1e-6f));
        }

        [Test]
        public void Food_WithNutrition_IgnoresEaterMultiplier()
        {
            _food.Capacity = 0.5f;
            _food.SetNutrition(0.07f);

            Assert.That(_food.GetGrowth(0.02f), Is.EqualTo(0.07f).Within(1e-6f));
            Assert.That(_food.GetGrowth(0.9f), Is.EqualTo(0.07f).Within(1e-6f));
        }

        [Test]
        public void Food_StoredAfterDrop_ForgetsNutrition()
        {
            _food.MarkAsDropped();
            _food.SetNutrition(0.05f);

            _food.MarkAsStored();

            Assert.That(_food.Nutrition, Is.EqualTo(0f));
        }

        [Test]
        public void Food_NegativeNutrition_IsClampedToZero()
        {
            _food.SetNutrition(-1f);

            Assert.That(_food.Nutrition, Is.EqualTo(0f));
        }

        [TestCase(0.01f, 4)]
        [TestCase(0.5f, 10)]
        [TestCase(2f, 30)]
        [TestCase(100f, 30)]
        public void Loot_ItemCountScalesWithMass(float capacity, int expected)
        {
            Assert.That(FoodSpawner.GetLootItemCount(capacity), Is.EqualTo(expected));
        }

        [Test]
        public void Kill_DoesNotCreateMass()
        {
            SerializedObject player = LoadPlayer();
            float absorbFraction = player.FindProperty("_killAbsorbFraction").floatValue;

            Assert.That(absorbFraction + FoodSpawner.LootMassFraction, Is.LessThanOrEqualTo(1f + 1e-4f),
                "Eater gain plus dropped loot must not exceed the victim's mass.");
            Assert.That(absorbFraction, Is.GreaterThan(0f), "Killing must be rewarding.");
        }

        [Test]
        public void Boost_CannotBeFarmedByEatingOwnTrail()
        {
            Assert.That(Player.BoostDropNutritionFraction, Is.InRange(0f, 0.99f));
        }

        private static SerializedObject LoadPlayer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            return new SerializedObject(prefab.GetComponent<Player>());
        }
    }
}
