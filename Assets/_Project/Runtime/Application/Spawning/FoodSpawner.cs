using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public sealed class FoodSpawner : Spawner<Food>, IRespawnService<Food>, ISpawnService<Food>, ILootSpawnService
	{
		private const float LootMassFraction = 0.6f;
		private const int LootItemCount = 6;

		[SerializeField, Min(0.1f)]
		private float _droppedPointLifetime = 10f;
		[SerializeField, Min(0.1f)]
		private float _lootLifetime = 20f;

		public void SpawnAt(Transform spawnTransform)
		{
			Food point = CreateObject(spawnTransform);

			int lifecycleVersion = point.MarkAsDropped();

			_ = ReleaseAfterLifetimeAsync(point, lifecycleVersion, _droppedPointLifetime);
		}

		public void SpawnLoot(Vector2 position, float sourceCapacity)
		{
			float itemMass = Mathf.Clamp(sourceCapacity * LootMassFraction / LootItemCount, 0.02f, 1f);
			float spreadRadius = Mathf.Max(sourceCapacity * 1.5f, 0.6f);
			float startAngle = Random.Range(0f, 360f / LootItemCount);

			for (int index = 0; index < LootItemCount; index++)
			{
				float angle = (startAngle + index * (360f / LootItemCount)) * Mathf.Deg2Rad;
				Vector2 itemPosition = position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spreadRadius;
				itemPosition = Constants.ClampToWorld(itemPosition);

				Food loot = CreateObject(itemPosition, itemMass);

				int lifecycleVersion = loot.MarkAsDropped();

				_ = ReleaseAfterLifetimeAsync(loot, lifecycleVersion, _lootLifetime);
			}
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
