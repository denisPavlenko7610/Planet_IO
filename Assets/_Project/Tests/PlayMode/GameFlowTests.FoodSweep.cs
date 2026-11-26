using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator FoodSweep_EatsFoodInsideTheBodyWithoutAnEnterEvent()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            FoodRecord food = PlaceFoodRelativeToPlayer(player, 0.5f);
            Vector2 placed = food.Position;
            float before = player.Capacity;

            RunFoodSweep(player);

            Assert.That(player.Capacity, Is.GreaterThan(before));
            Assert.That(Vector2.Distance(food.Position, placed), Is.GreaterThan(0.01f), "Eaten food is respawned elsewhere.");
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator FoodSweep_LeavesFoodOutsideTheBodyAlone()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            FoodRecord food = PlaceFoodRelativeToPlayer(player, 3f);
            Vector2 placed = food.Position;

            RunFoodSweep(player);

            Assert.That(Vector2.Distance(food.Position, placed), Is.LessThan(0.001f));
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator FoodSweep_DoesNotEatFoodAlreadyClaimed()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            FoodRecord food = PlaceFoodRelativeToPlayer(player, 0.5f);
            Vector2 placed = food.Position;
            Assert.That(food.TryClaim(), Is.True);
            float before = player.Capacity;

            RunFoodSweep(player);

            Assert.That(Vector2.Distance(food.Position, placed), Is.LessThan(0.001f));
            Assert.That(food.IsDropped, Is.False);
            Assert.That(player.Capacity, Is.GreaterThanOrEqualTo(before));
            food.ResetClaim();
        }

        private static FoodRecord PlaceFoodRelativeToPlayer(Player player, float bodyRadiusFactor)
        {
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            FoodRecord food = AllFood()
                .First(candidate => !candidate.IsDropped && candidate.Active);
            food.Position = player.transform.position + Vector3.right * (player.BodyRadius * bodyRadiusFactor);
            Physics2D.SyncTransforms();
            return food;
        }

        private static void RunFoodSweep(Player player)
        {
            SetMoonField(player, "_bodyFoodSweepTimer", FoodSweepRules.Interval);
            typeof(PlanetBody).GetMethod("SweepFoodInsideBody", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, new object[] { Time.fixedDeltaTime });
        }
    }
}
