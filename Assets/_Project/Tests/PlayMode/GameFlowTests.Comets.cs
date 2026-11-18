using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Comets_HalvePlayerAndBotMassAndCurrentScoreAtAnyCometSize()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
            Collider2D cometCollider = comet.GetComponent<Collider2D>();
            MethodInfo enemyTouch = typeof(Enemy).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (float cometMass in new[] { 0.1f, 2f })
            {
                foreach (float planetMass in new[] { 0.2f, 0.8f, 2f })
                {
                    comet.Capacity = cometMass;
                    player.Capacity = planetMass;
                    int scoreBefore = Constants.CapacityToScore(player.Capacity);
                    TouchWith(player, cometCollider);
                    Assert.That(player.Capacity, Is.EqualTo(planetMass * 0.5f).Within(1e-5f));
                    Assert.That(Constants.CapacityToScore(player.Capacity), Is.EqualTo(scoreBefore / 2));
                    Assert.That(player.IsDefeated, Is.False);

                    comet.Capacity = cometMass;
                    enemy.Capacity = planetMass;
                    enemyTouch.Invoke(enemy, new object[] { cometCollider });
                    Assert.That(enemy.Capacity, Is.EqualTo(planetMass * 0.5f).Within(1e-5f));
                }
            }
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Comets_AtMinimumMassRetainExistingDefeatRule()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = player.MinCapacity * 1.5f;
            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();

            TouchWith(player, comet.GetComponent<Collider2D>());

            Assert.That(player.IsDefeated, Is.True);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Comets_MoveShowNamesAndClearTailOnRespawn()
        {
            yield return StartMoonGame();
            Player player = FindLocalPlayer();
            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
            CometMovement movement = comet.GetComponent<CometMovement>();
            movement.Randomize();
            Vector2 start = comet.transform.position;
            Assert.That(CometRules.IsOutsideView(start, player.transform.position), Is.True);

            movement.Launch(new Vector2(150f, 100f), Vector2.right, 3f);
            yield return new WaitForSeconds(0.4f);
            Assert.That(comet.GetComponent<Rigidbody2D>().linearVelocity.magnitude, Is.EqualTo(3f).Within(0.01f));
            TextMeshPro label = comet.GetComponentInChildren<TextMeshPro>();
            Assert.That(label.enabled, Is.True);
            Assert.That(label.text, Is.Not.Empty);
            TrailRenderer tail = comet.GetComponentInChildren<TrailRenderer>();
            Assert.That(tail, Is.Not.Null);
            Assert.That(tail.sharedMaterial, Is.Not.Null);
            Assert.That(tail.positionCount, Is.GreaterThan(1));
            Assert.That(tail.widthCurve.Evaluate(0f), Is.GreaterThan(tail.widthCurve.Evaluate(1f)));

            var camera = UnityEngine.Camera.main;
            var follow = camera.GetComponent<PlanetIO.UI.Camera.PlayerCamera>();
            if (follow != null) follow.enabled = false;
            camera.transform.position = comet.transform.position + new Vector3(-1.5f, 0f, -10f);
            camera.orthographicSize = 3.6f;
            yield return new WaitForSeconds(1.2f);
            camera.transform.position = comet.transform.position + new Vector3(-1.5f, 0f, -10f);
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Library/CometPreview.png"));
            yield return new WaitForSeconds(0.2f);

            ResolveGame<IRespawnService<Comet>>().Respawn(comet);
            Assert.That(tail.positionCount, Is.Zero);
            Assert.That(CometRules.IsOutsideView(comet.transform.position, player.transform.position), Is.True);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Comets_FoodReusesAmbientMassHonorsBudgetAndReturnsToFields()
        {
            yield return StartMoonGame();
            FoodSpawner spawner = Object.FindFirstObjectByType<FoodSpawner>();
            var trail = (Dictionary<FoodRecord, float>)typeof(FoodSpawner)
                .GetField("_cometFood", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            foreach (FoodRecord food in trail.Keys.ToArray()) spawner.Respawn(food);
            FoodRecord[] before = AllFood();
            var positions = before.ToDictionary(food => food, food => food.Position);
            float totalMass = before.Sum(food => food.GetGrowth(0.002f));
            Vector2 destination = new(170f, 110f);
            for (int index = 0; index < CometRules.MaximumTrailFood + 5; index++)
            {
                spawner.TryLeaveCometFood(destination);
            }

            Assert.That(trail.Count, Is.InRange(1, CometRules.MaximumTrailFood));
            FoodRecord[] after = AllFood();
            Assert.That(after.Length, Is.EqualTo(before.Length));
            Assert.That(after.Sum(food => food.GetGrowth(0.002f)), Is.EqualTo(totalMass).Within(1e-5f));
            foreach (FoodRecord food in trail.Keys)
            {
                Assert.That(food.ValueMultiplier, Is.EqualTo(1f));
                Assert.That(food.IsDropped, Is.False);
                Assert.That(food.DensityCell, Is.EqualTo(-1));
                Assert.That(food.Position, Is.Not.EqualTo(positions[food]));
            }

            FoodRecord[] moved = trail.Keys.ToArray();
            spawner.World.Advance(CometRules.FoodLifetime + 0.01f);
            typeof(FoodSpawner).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
            Assert.That(trail.Count, Is.Zero);
            Assert.That(moved.All(food => food.DensityCell >= 0), Is.True);
        }
    }
}
