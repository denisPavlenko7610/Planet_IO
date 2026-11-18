using NUnit.Framework;
using PlanetIO;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class EnemyDecisionRulesTests
    {
        [Test]
        public void Hazard_AlwaysHasHighestPriority()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                2f,
                0.5f,
                true,
                true,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Evade));
        }

        [Test]
        public void BiggerOpponent_HuntsPlayer()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1.5f,
                1f,
                true,
                false,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Hunt));
        }

        [Test]
        public void SmallerOpponent_EvadesPlayer()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f,
                1.5f,
                true,
                false,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Evade));
        }

        [Test]
        public void Food_IsSelectedWhenPlayerIsNotActionable()
        {
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
                1f,
                0.98f,
                true,
                false,
                1.1f,
                1.05f);

            Assert.That(intent, Is.EqualTo(EnemyIntent.Forage));
        }

        [Test]
        public void CapacitySpeedMultiplier_NeverFallsBelowMinimum()
        {
            float multiplier =
                EnemyDecisionRules.GetCapacitySpeedMultiplier(
                    100f,
                    0.08f,
                    0.3f,
                    0.55f);

            Assert.That(multiplier, Is.EqualTo(0.55f).Within(0.0001f));
        }

        [Test]
        public void Think_SelectsNearestFoodAndIgnoresNonFoodEntities()
        {
            using var scope = new SensingScope();
            scope.AddFood(Vector2.up, false);
            scope.AddFood(Vector2.left * 6f);
            scope.AddFood(Vector2.right * 2f);

            scope.Think();

            Assert.That(scope.Movement.State, Is.EqualTo(EnemyMovement.MovementState.Foraging));
            Assert.That(scope.DesiredDirection, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void Think_EqualDistanceFoodUsesGridOrder()
        {
            using var scope = new SensingScope();
            scope.AddFood(Vector2.left * 3f);
            scope.AddFood(Vector2.right * 3f);
			var candidates = new List<FoodRecord>();
			scope.FoodService.Query(scope.Origin, 16f, candidates);
			Assert.That(candidates.Count, Is.EqualTo(2));
			Vector2 expected = (candidates[0].Position - scope.Origin).normalized;

            scope.Think();

            Assert.That(scope.DesiredDirection, Is.EqualTo(expected));
        }

        [Test]
        public void Think_FoodOutsideSearchRadiusDoesNotBecomeTarget()
        {
            using var scope = new SensingScope();
            scope.AddFood(Vector2.right * 18f);

            scope.Think();

            Assert.That(scope.Movement.State, Is.EqualTo(EnemyMovement.MovementState.Roaming));
        }

        private sealed class SensingScope : IDisposable
        {
            private readonly List<GameObject> _objects = new();
            private readonly UnityEngine.Random.State _randomState = UnityEngine.Random.state;
            public readonly Vector2 Origin = new(10000f, 10000f);
            public readonly EnemyMovement Movement;
			public readonly FoodServiceStub FoodService = new();

            public SensingScope()
            {
                var root = new GameObject("SensingEnemy");
                _objects.Add(root);
                root.transform.position = Origin;
                root.AddComponent<CircleCollider2D>();
                Enemy enemy = root.AddComponent<Enemy>();
                Movement = root.GetComponent<EnemyMovement>();
				typeof(EnemyMovement).GetField("_foodSpawner", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(Movement, FoodService);
                typeof(Enemy).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(enemy, null);
                typeof(EnemyMovement).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Movement, null);
            }

            public Vector2 DesiredDirection => (Vector2)typeof(EnemyMovement)
                .GetField("_desiredDirection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Movement);

            public void AddFood(Vector2 offset, bool onFoodLayer = true)
            {
				if (onFoodLayer) FoodService.World.Create(Origin + offset, 0.2f);
            }

            public void Think()
            {
                Physics2D.SyncTransforms();
                typeof(EnemyMovement).GetMethod("Think", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(Movement, null);
            }

            public void Dispose()
            {
                foreach (GameObject root in _objects) Object.DestroyImmediate(root);
                UnityEngine.Random.state = _randomState;
            }
        }
    }
}
