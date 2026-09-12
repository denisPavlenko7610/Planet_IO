using UnityEngine;
using System;

namespace PlanetIO
{
    public sealed class FoodSpawner : Spawner<Food>, IRespawnService<Food>, ISpawnService<Food>
	{
		[SerializeField, Min(0.1f)]
		private float _droppedPointLifetime = 10f;

		public void SpawnAt(Transform spawnTransform)
		{
			Food point = CreateObject(spawnTransform);

			int lifecycleVersion = point.MarkAsDropped();

			_ = ReleaseAfterLifetimeAsync(point, lifecycleVersion);
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

		private async Awaitable ReleaseAfterLifetimeAsync(Food point, int lifecycleVersion)
		{
			try
			{
				await Awaitable.WaitForSecondsAsync(_droppedPointLifetime, destroyCancellationToken);

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
