using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
	public interface IFoodService : IRespawnService<FoodRecord>, ISpawnService<FoodRecord>
	{
		FoodWorld World { get; }
		void Query(Vector2 position, float radius, List<FoodRecord> result);
		void PlayEatenEffect(FoodRecord food);
		void RestoreDroppers(IReadOnlyDictionary<ulong, ulong> ids);
	}
}
