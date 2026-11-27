using UnityEngine;

namespace PlanetIO
{
	public static class FoodPickupRules
	{
		public static bool Intersects(Vector2 from, Vector2 to, Vector2 food, float reach)
		{
			Vector2 segment = to - from;
			float ratio = segment.sqrMagnitude > 0.000001f ? Mathf.Clamp01(Vector2.Dot(food - from, segment) / segment.sqrMagnitude) : 0f;
			return (food - (from + segment * ratio)).sqrMagnitude <= reach * reach;
		}
	}
}
