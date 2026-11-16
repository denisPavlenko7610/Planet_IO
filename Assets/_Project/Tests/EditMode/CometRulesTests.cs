using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class CometRulesTests
    {
        [Test]
        public void Spawn_ClearanceIncludesBoundary()
        {
            Vector2 player = new(15f, -20f);
            Assert.That(CometRules.IsOutsideView(player + Vector2.right * 55f, player), Is.False);
            Assert.That(CometRules.IsOutsideView(player + Vector2.up * 56f, player), Is.True);
        }

        [Test]
        public void Food_StopsAtGlobalBudgetAndOutsideArena()
        {
            Assert.That(CometRules.CanLeaveFood(89, Vector2.zero), Is.True);
            Assert.That(CometRules.CanLeaveFood(90, Vector2.zero), Is.False);
            Assert.That(CometRules.CanLeaveFood(0, new Vector2(-1000f, 0f)), Is.False);
        }

        [TestCase(0.1f)]
        [TestCase(1.5f)]
        public void Food_IsBehindBodyForAnyFlightDirection(float radius)
        {
            Vector2 direction = new Vector2(-2f, 3f).normalized;
            Vector2 position = new(100f, 50f);
            Vector2 offset = CometRules.FoodPosition(position, direction, radius) - position;
            Assert.That(Vector2.Dot(offset.normalized, direction), Is.EqualTo(-1f).Within(1e-5f));
            Assert.That(offset.magnitude, Is.GreaterThan(radius));
        }
    }
}
