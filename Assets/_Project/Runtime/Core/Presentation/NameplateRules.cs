using UnityEngine;

namespace PlanetIO
{
	public static class NameplateRules
	{
		public const float ReferenceOrthographicSize = 3.6f;
		public const float LabelScale = 0.55f;

		public static float ZoomRatio(float orthographicSize)
		{
			return Mathf.Max(0.01f, orthographicSize) / ReferenceOrthographicSize;
		}

		public static float WorldScale(float orthographicSize)
		{
			return LabelScale * ZoomRatio(orthographicSize);
		}
	}
}
