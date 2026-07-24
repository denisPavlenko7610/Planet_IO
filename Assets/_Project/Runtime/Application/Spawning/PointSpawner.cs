using UnityEngine;
using System;

namespace PlanetIO
{
    public sealed class PointSpawner : Spawner<Point>, IRespawnService<Point>, ISpawnService<Point>
	{
		[SerializeField, Min(0.1f)]
		private float _droppedPointLifetime = 10f;

		public void SpawnAt(Transform spawnTransform)
		{
			Point point = CreateObject(spawnTransform);

			int lifecycleVersion = point.MarkAsDropped();

			_ = ReleaseAfterLifetimeAsync(point, lifecycleVersion);
		}

		public void Respawn(Point point)
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
		}

		private void ReturnDroppedPoint(Point point)
		{
			point.MarkAsStored();
			ReleaseObject(point);
		}

		private async Awaitable ReleaseAfterLifetimeAsync(Point point, int lifecycleVersion)
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
				LoggerIO.LogError("Scene or spawner was destroyed");
			}
		}
    }
}
