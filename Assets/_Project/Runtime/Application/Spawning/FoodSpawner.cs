using UnityEngine;
using System;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public sealed class FoodSpawner : Spawner<Food>, IRespawnService<Food>, ISpawnService<Food>, ILootSpawnService
	{
		private const float LootMassFraction = 0.6f;
		private const int LootItemCount = 6;
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
