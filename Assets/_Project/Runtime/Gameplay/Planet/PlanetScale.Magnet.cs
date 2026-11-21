using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
	public abstract partial class PlanetScale
	{
		private const int MagnetBufferSize = 64;

		private readonly List<FoodRecord> _magnetFood = new(MagnetBufferSize);

		protected virtual void ServerFeedingStep(float deltaTime)
		{
		}

		protected void PullFoodTowardsBody(float baseReach, float perkMultiplier, float pullSpeed, float deltaTime)
		{
			if (FoodRespawnService == null)
			{
				return;
			}

			Vector2 center = transform.position;
			float reach = FoodMagnetRules.Reach(BodyRadius, baseReach, perkMultiplier);
			FoodRespawnService.Query(center, reach, _magnetFood);
			float step = pullSpeed * deltaTime;

			foreach (FoodRecord food in _magnetFood)
			{
				if (food.IsMagnetImmuneFor(NetworkObjectId))
				{
					continue;
				}

				food.Position = Vector2.MoveTowards(food.Position, center, step);
			}
		}
	}
}
