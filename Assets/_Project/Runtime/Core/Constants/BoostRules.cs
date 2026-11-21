using UnityEngine;

namespace PlanetIO
{
	public static class BoostRules
	{
		public const float MassFractionPerTick = 0.002f;

		public static float Cost(float capacity, float flatCost, float costMultiplier)
		{
			float proportional = Mathf.Max(0f, capacity) * MassFractionPerTick;

			return Mathf.Max(flatCost, proportional) * Mathf.Max(0f, costMultiplier);
		}
	}
}
