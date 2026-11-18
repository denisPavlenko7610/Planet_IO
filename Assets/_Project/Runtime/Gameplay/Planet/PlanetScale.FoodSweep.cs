using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
	public abstract partial class PlanetScale
	{
		private readonly List<FoodRecord> _bodyFood = new(128);
		private float _bodyFoodSweepTimer;
		private Vector2 _previousFoodSweep;
		private bool _hasFoodSweepPosition;
		private void SweepFoodInsideBody(float deltaTime)
		{
			if (FoodRespawnService == null || !FoodSweepRules.Advance(ref _bodyFoodSweepTimer, deltaTime)) return;
			Vector2 current = transform.position;
			Vector2 previous = _hasFoodSweepPosition && (current - _previousFoodSweep).sqrMagnitude <= 16f ? _previousFoodSweep : current;
			_previousFoodSweep = current;
			_hasFoodSweepPosition = true;
			FoodRespawnService.Query((current + previous) * 0.5f, BodyRadius + Vector2.Distance(current, previous) * 0.5f, _bodyFood);
			foreach (FoodRecord food in _bodyFood)
			{
				if (FoodPickupRules.Intersects(previous, current, food.Position, BodyRadius + food.Radius)) TryEatFood(food);
			}
		}
	}
}
