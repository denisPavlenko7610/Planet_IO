using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO.Tests
{
	internal sealed class FoodServiceStub : IFoodService
	{
		public FoodWorld World { get; } = new(true, Constants.WorldBounds);
		public void Query(Vector2 position, float radius, List<FoodRecord> result) => World.Query(position, radius, result);
		public void Respawn(FoodRecord food) => World.Remove(food);
		public FoodRecord SpawnAt(Transform position, float nutrition)
		{
			FoodRecord record = World.Create(position.position, 0.2f);
			record.SetNutrition(nutrition);
			return record;
		}
		public void PlayEatenEffect(FoodRecord food) { }
		public void RestoreDroppers(IReadOnlyDictionary<ulong, ulong> ids) { }
	}
}
