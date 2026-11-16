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
        [UnityTest, Timeout(45000)]
        public IEnumerator FoodSweep_EatsFoodInsideTheBodyWithoutAnEnterEvent()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            Food food = PlaceFoodRelativeToPlayer(player, 0.5f);
            Vector2 placed = food.transform.position;
            float before = player.Capacity;

            RunFoodSweep(player);

            Assert.That(player.Capacity, Is.GreaterThan(before));
            Assert.That(Vector2.Distance(food.transform.position, placed), Is.GreaterThan(0.01f), "Eaten food is respawned elsewhere.");
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator FoodSweep_LeavesFoodOutsideTheBodyAlone()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            Food food = PlaceFoodRelativeToPlayer(player, 3f);
            Vector2 placed = food.transform.position;

            RunFoodSweep(player);

            Assert.That(Vector2.Distance(food.transform.position, placed), Is.LessThan(0.001f));
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator FoodSweep_DoesNotEatFoodAlreadyClaimed()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            Food food = PlaceFoodRelativeToPlayer(player, 0.5f);
            Vector2 placed = food.transform.position;
            Assert.That(food.TryClaim(), Is.True);
            float before = player.Capacity;

            RunFoodSweep(player);

            Assert.That(Vector2.Distance(food.transform.position, placed), Is.LessThan(0.001f));
            Assert.That(food.IsDropped, Is.False);
            Assert.That(player.Capacity, Is.GreaterThanOrEqualTo(before));
        }

        private static Food PlaceFoodRelativeToPlayer(Player player, float bodyRadiusFactor)
        {
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            Food food = Object.FindObjectsByType<Food>(FindObjectsInactive.Exclude)
                .First(candidate => !candidate.IsDropped && candidate.gameObject.activeInHierarchy);
            food.transform.position = player.transform.position + Vector3.right * (player.BodyRadius * bodyRadiusFactor);
            Physics2D.SyncTransforms();
            return food;
        }

        private static void RunFoodSweep(Player player)
        {
            SetMoonField(player, "_bodyFoodSweepTimer", FoodSweepRules.Interval);
            typeof(PlanetScale).GetMethod("SweepFoodInsideBody", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(player, new object[] { Time.fixedDeltaTime });
        }
    }
}
