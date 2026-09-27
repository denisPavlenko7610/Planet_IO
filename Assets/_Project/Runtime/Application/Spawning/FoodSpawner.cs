using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public sealed class FoodSpawner : Spawner<Food>, IRespawnService<Food>, ISpawnService<Food>, ILootSpawnService
	{
		public const float LootMassFraction = 0.6f;
		private const float LootItemsPerCapacity = 20f;
		private const int MinimumLootItems = 4;
		private const int MaximumLootItems = 30;
		private const float MinimumLootScale = 0.7f;
		private const float MaximumLootScale = 1.6f;
		private const float LootScalePerNutrition = 10f;
		private const float LootTintMultiplier = 2f;
		private const float GoldenValueMultiplier = 8f;
		private const float ClusterJitter = 7f;

		[SerializeField, Min(0.1f)]
		private float _droppedPointLifetime = 10f;
		[SerializeField, Min(0.1f)]
		private float _lootLifetime = 20f;
		[SerializeField, Range(0f, 1f)]
		private float _goldenChance = 0.04f;

		private Vector2 _clusterCenter;
		private int _clusterRemaining;

		public void SpawnAt(Transform spawnTransform, float nutrition)
		{
			Food point = CreateObject(spawnTransform);

			int lifecycleVersion = point.MarkAsDropped();
			point.SetNutrition(nutrition);
			point.SetValueMultiplier(1f);

			_ = ReleaseAfterLifetimeAsync(point, lifecycleVersion, _droppedPointLifetime);
		}

		public void SpawnLoot(Vector2 position, float sourceCapacity)
		{
			int itemCount = GetLootItemCount(sourceCapacity);
			float itemNutrition = sourceCapacity * LootMassFraction / itemCount;
			float itemScale = Mathf.Clamp(
				MinimumLootScale + itemNutrition * LootScalePerNutrition, MinimumLootScale, MaximumLootScale);
			float spreadRadius = Mathf.Max(sourceCapacity * 1.5f, 0.6f);
			float startAngle = Random.Range(0f, 360f / itemCount);

			for (int index = 0; index < itemCount; index++)
			{
				float angle = (startAngle + index * (360f / itemCount)) * Mathf.Deg2Rad;
				float distance = spreadRadius * Random.Range(0.5f, 1.2f);
				Vector2 itemPosition = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
				itemPosition = Constants.ClampToWorld(itemPosition);

				Food loot = CreateObject(itemPosition, itemScale);

				int lifecycleVersion = loot.MarkAsDropped();
				loot.SetNutrition(itemNutrition);
				loot.SetValueMultiplier(LootTintMultiplier);

				_ = ReleaseAfterLifetimeAsync(loot, lifecycleVersion, _lootLifetime);
			}
		}

		public static int GetLootItemCount(float sourceCapacity)
		{
			return Mathf.Clamp(
				Mathf.RoundToInt(sourceCapacity * LootItemsPerCapacity), MinimumLootItems, MaximumLootItems);
		}

		protected override Vector2 GetRandomPosition()
		{
			if (_clusterRemaining <= 0)
			{
				_clusterCenter = base.GetRandomPosition();
				_clusterRemaining = Random.Range(3, 6);
			}

			_clusterRemaining--;
			return Constants.ClampToWorld(_clusterCenter + Random.insideUnitCircle * ClusterJitter);
		}

		public void Respawn(Food point)
		{
			if (point == null)
			{
				return;
			}

			if (point.IsDropped)
			{
				ReturnDroppedPoint(point);
				return;
			}

			RespawnObject(point);
			point.ResetClaim();
			point.SetValueMultiplier(Random.value < _goldenChance ? GoldenValueMultiplier : 1f);
		}

		private void ReturnDroppedPoint(Food point)
		{
			point.MarkAsStored();
			ReleaseObject(point);
		}

		private async Awaitable ReleaseAfterLifetimeAsync(Food point, int lifecycleVersion, float lifetime)
		{
			try
			{
				await Awaitable.WaitForSecondsAsync(lifetime, destroyCancellationToken);

				if (point == null ||
					!point.IsDropped ||
					point.LifecycleVersion != lifecycleVersion)
				{
					return;
				}

				ReturnDroppedPoint(point);
			}
			catch (OperationCanceledException)
			{
				GameLogger.Log("Dropped point lifetime cancelled.");
			}
		}
    }
}
