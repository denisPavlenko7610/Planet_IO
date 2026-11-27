using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class BoostDropRulesTests
    {
        [TestCase(0.08f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        [TestCase(2f)]
        public void SpawnPosition_IsOutsideBodyAtAnyCapacity(float capacity)
        {
            float bodyRadius = 3.55f * capacity;

            Vector2 position = BoostDropRules.SpawnPosition(Vector2.zero, Vector2.right, bodyRadius);

            Assert.That(position.magnitude, Is.GreaterThan(bodyRadius + 0.2f));
        }

        [Test]
        public void SpawnPosition_IsBehindTravelDirection()
        {
            Vector2 position = BoostDropRules.SpawnPosition(new Vector2(10f, 5f), new Vector2(0f, 3f), 1f);

            Assert.That(position.x, Is.EqualTo(10f).Within(1e-5f));
            Assert.That(position.y, Is.EqualTo(5f - 1f - BoostDropRules.EdgeClearance).Within(1e-5f));
        }

        [Test]
        public void SpawnPosition_WithZeroDirection_StaysOutsideBody()
        {
            Vector2 position = BoostDropRules.SpawnPosition(Vector2.zero, Vector2.zero, 2f);

            Assert.That(position.magnitude, Is.EqualTo(2f + BoostDropRules.EdgeClearance).Within(1e-5f));
        }
    }
}
