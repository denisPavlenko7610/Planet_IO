using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class PerformanceRulesTests
    {
        [Test]
        public void ExpiryQueue_ReturnsOnlyExpiredItemsInExpiryOrder()
        {
            var queue = new ExpiryQueue<string>();
            queue.Enqueue("late", 1, 20f);
            queue.Enqueue("early", 2, 5f);
            queue.Enqueue("middle", 3, 10f);

            Assert.That(queue.TryDequeueExpired(4f, out _, out _), Is.False);

            Assert.That(queue.TryDequeueExpired(12f, out string first, out int firstVersion), Is.True);
            Assert.That(first, Is.EqualTo("early"));
            Assert.That(firstVersion, Is.EqualTo(2));

            Assert.That(queue.TryDequeueExpired(12f, out string second, out int secondVersion), Is.True);
            Assert.That(second, Is.EqualTo("middle"));
            Assert.That(secondVersion, Is.EqualTo(3));

            Assert.That(queue.TryDequeueExpired(12f, out _, out _), Is.False);
            Assert.That(queue.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExpiryQueue_KeepsInsertionOrderForEqualExpiry()
        {
            var queue = new ExpiryQueue<int>();
            for (int index = 0; index < 8; index++)
            {
                queue.Enqueue(index, 0, 3f);
            }

            for (int index = 0; index < 8; index++)
            {
                Assert.That(queue.TryDequeueExpired(3f, out int item, out _), Is.True);
                Assert.That(item, Is.EqualTo(index));
            }
        }

        [Test]
        public void ExpiryQueue_HandlesRandomExpiriesAndRestoredItemsOutOfOrder()
        {
            var queue = new ExpiryQueue<int>();
            var random = new System.Random(7);
            float[] expiries = new float[500];
            for (int index = 0; index < expiries.Length; index++)
            {
                expiries[index] = (float)random.NextDouble() * 20f;
                queue.Enqueue(index, 0, expiries[index]);
            }

            float previous = -1f;
            int drained = 0;
            while (queue.TryDequeueExpired(20f, out int item, out _))
            {
                Assert.That(expiries[item], Is.GreaterThanOrEqualTo(previous));
                previous = expiries[item];
                drained++;
            }

            Assert.That(drained, Is.EqualTo(expiries.Length));
            Assert.That(queue.Count, Is.Zero);
        }

        [Test]
        public void ExpiryQueue_ClearEmptiesTheQueue()
        {
            var queue = new ExpiryQueue<int>();
            queue.Enqueue(1, 0, 1f);
            queue.Clear();

            Assert.That(queue.Count, Is.Zero);
            Assert.That(queue.TryDequeueExpired(100f, out _, out _), Is.False);
        }

        [Test]
        public void ViewCullRules_IncludesBodiesWhoseRadiusTouchesTheRectangle()
        {
            Rect view = new(-10f, -5f, 20f, 10f);

            Assert.That(ViewCullRules.Intersects(view, Vector2.zero, 0.1f), Is.True);
            Assert.That(ViewCullRules.Intersects(view, new Vector2(12f, 0f), 3f), Is.True);
            Assert.That(ViewCullRules.Intersects(view, new Vector2(14f, 0f), 3f), Is.False);
            Assert.That(ViewCullRules.Intersects(view, new Vector2(0f, -9f), 3f), Is.False);
        }

        [Test]
        public void ViewBounds_TreatsEverythingAsVisibleUntilTheCameraPublishesARectangle()
        {
            ViewBounds.Clear();
            Assert.That(ViewBounds.IsVisible(new Vector2(1000f, 1000f), 0f), Is.True);

            ViewBounds.Set(new Rect(-10f, -5f, 20f, 10f));
            try
            {
                Assert.That(ViewBounds.IsVisible(new Vector2(1000f, 1000f), 0f), Is.False);
                Assert.That(ViewBounds.IsVisible(new Vector2(11.5f, 0f), 0f), Is.True);
            }
            finally
            {
                ViewBounds.Clear();
            }
        }

        [Test]
        public void EatEffectRules_SendsOnlyToViewersInsideTheRectangle()
        {
            Vector2 effect = new(100f, 50f);

            Assert.That(EatEffectRules.IsRelevant(effect, new Vector2(100f, 50f)), Is.True);
            Assert.That(EatEffectRules.IsRelevant(effect, new Vector2(100f + EatEffectRules.RelevanceHalfWidth, 50f)), Is.True);
            Assert.That(EatEffectRules.IsRelevant(effect, new Vector2(100f + EatEffectRules.RelevanceHalfWidth + 1f, 50f)), Is.False);
            Assert.That(EatEffectRules.IsRelevant(effect, new Vector2(100f, 50f - EatEffectRules.RelevanceHalfHeight - 1f)), Is.False);
        }

        [TestCase(2048, 8, DeviceTier.Low)]
        [TestCase(3500, 8, DeviceTier.Low)]
        [TestCase(6000, 4, DeviceTier.Low)]
        [TestCase(6000, 8, DeviceTier.Standard)]
        [TestCase(0, 0, DeviceTier.Standard)]
        public void DeviceTierRules_ClassifiesWeakDevices(int memory, int cores, DeviceTier expected)
        {
            Assert.That(DeviceTierRules.Resolve(memory, cores), Is.EqualTo(expected));
        }

        [Test]
        public void DeviceTierRules_ClampsQualityLevelToTheAvailableLevels()
        {
            Assert.That(DeviceTierRules.QualityLevel(DeviceTier.Low, 3), Is.EqualTo(DeviceTierRules.LowQualityLevel));
            Assert.That(DeviceTierRules.QualityLevel(DeviceTier.Standard, 3), Is.EqualTo(DeviceTierRules.StandardQualityLevel));
            Assert.That(DeviceTierRules.QualityLevel(DeviceTier.Standard, 1), Is.Zero);
        }
    }
}
