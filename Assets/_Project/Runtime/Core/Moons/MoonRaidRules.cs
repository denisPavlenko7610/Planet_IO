using UnityEngine;

namespace PlanetIO
{
	public static class MoonRaidRules
	{
		public const float CommitmentSeconds = 1.5f;
		public const float CooldownSeconds = 4f;

		public static bool CanRaid(
			float ownMass,
			float targetMass,
			bool armed,
			BotDifficulty difficulty,
			bool cautious,
			bool hazard,
			float cooldown
		)
		{
			return armed &&
				!hazard &&
				cooldown <= 0f &&
				difficulty != BotDifficulty.Easy &&
				(difficulty == BotDifficulty.Hard || !cautious) &&
				ownMass > 0f &&
				targetMass >= ownMass * CombatRules.DefaultEatSizeRatio &&
				targetMass <= ownMass * 3f;
		}

		public static float StandOff(float ownRadius, float targetRadius)
		{
			float gap = MoonRules.OuterExtent(ownRadius) - ownRadius;
			return ownRadius + targetRadius + gap * 0.55f;
		}

		public static bool ShouldRetreat(float remaining, float initialMass, float currentMass, bool armed)
		{
			return remaining <= 0f || !armed || currentMass < initialMass - 0.0001f;
		}

		public static Vector2 AvoidCore(
			Vector2 position,
			Vector2 target,
			float safeDistance,
			Vector2 direction,
			float step
		)
		{
			Vector2 away = position - target;
			if (away.sqrMagnitude < safeDistance * safeDistance)
				return away.normalized;
			if ((away + direction * step).sqrMagnitude >= safeDistance * safeDistance)
				return direction;

			Vector2 normal = away.normalized;
			Vector2 tangent = direction - normal * Vector2.Dot(direction, normal);
			return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : new Vector2(-normal.y, normal.x);
		}
	}
}