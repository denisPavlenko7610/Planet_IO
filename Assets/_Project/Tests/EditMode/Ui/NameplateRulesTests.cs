using NUnit.Framework;

namespace PlanetIO.Tests
{
	public sealed class NameplateRulesTests
	{
		[TestCase(3.6f)]
		[TestCase(7f)]
		[TestCase(14.04f)]
		[TestCase(30f)]
		public void WorldScale_KeepsConstantScreenSizeAtAnyZoom(float orthographicSize)
		{
			float screenSize = NameplateRules.WorldScale(orthographicSize) / orthographicSize;

			Assert.That(screenSize, Is.EqualTo(NameplateRules.LabelScale / NameplateRules.ReferenceOrthographicSize).Within(1e-5f));
		}

		[Test]
		public void WorldScale_DoesNotCollapseAtZeroZoom()
		{
			Assert.That(NameplateRules.WorldScale(0f), Is.GreaterThan(0f));
		}
	}
}
