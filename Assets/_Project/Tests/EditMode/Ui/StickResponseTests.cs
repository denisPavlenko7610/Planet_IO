using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class StickResponseTests
    {
        private const float ProcessorMin = 0.125f;
        private const float ProcessorMax = 0.925f;

        [Test]
        public void ApplyDeadZone_InsideDeadZone_IsZero()
        {
            Assert.That(StickResponse.ApplyDeadZone(new Vector2(0.15f, 0.1f), 0.2f), Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ApplyDeadZone_JustOutside_StartsNearZero()
        {
            Vector2 result = StickResponse.ApplyDeadZone(new Vector2(0.21f, 0f), 0.2f);

            Assert.That(result.magnitude, Is.InRange(0f, 0.05f));
        }

        [Test]
        public void ApplyDeadZone_FullTilt_IsOne()
        {
            Assert.That(StickResponse.ApplyDeadZone(new Vector2(0f, 1f), 0.2f).magnitude, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void ApplyDeadZone_KeepsDirection()
        {
            Vector2 result = StickResponse.ApplyDeadZone(new Vector2(0.6f, 0.6f), 0.2f);

            Assert.That(result.x, Is.EqualTo(result.y).Within(1e-5f));
            Assert.That(result.x, Is.GreaterThan(0f));
        }

        [TestCase(0.05f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void PreCompensate_SurvivesStickDeadzoneProcessor(float shapedMagnitude)
        {
            Vector2 compensated = StickResponse.PreCompensate(new Vector2(shapedMagnitude, 0f), ProcessorMin, ProcessorMax);

            Assert.That(ProcessStickDeadzone(compensated.magnitude), Is.EqualTo(shapedMagnitude).Within(1e-4f));
        }

        [Test]
        public void PreCompensate_Zero_StaysZero()
        {
            Assert.That(StickResponse.PreCompensate(Vector2.zero, ProcessorMin, ProcessorMax), Is.EqualTo(Vector2.zero));
        }

        private static float ProcessStickDeadzone(float magnitude)
        {
            if (magnitude < ProcessorMin)
            {
                return 0f;
            }

            return magnitude > ProcessorMax ? 1f : (magnitude - ProcessorMin) / (ProcessorMax - ProcessorMin);
        }
    }
}
