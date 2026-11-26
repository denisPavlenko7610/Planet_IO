using UnityEngine;

namespace PlanetIO
{
	public static class WorldBounds
	{
		public static readonly Rect Area = new(-220f, -136f, 440f, 296f);

		public static Vector3 Clamp(Vector3 position)
		{
			position.x = Mathf.Clamp(position.x, Area.xMin, Area.xMax);
			position.y = Mathf.Clamp(position.y, Area.yMin, Area.yMax);

			return position;
		}

		public static Vector2 RandomPosition(float margin = 5f)
		{
			return new Vector2(
				Random.Range(Area.xMin + margin, Area.xMax - margin),
				Random.Range(Area.yMin + margin, Area.yMax - margin));
		}

		public static Vector2 KeepInside(Vector2 position, Vector2 velocity)
		{
			Rect bounds = Area;
			if (position.x <= bounds.xMin && velocity.x < 0f || position.x >= bounds.xMax && velocity.x > 0f)
			{
				velocity.x = 0f;
			}

			if (position.y <= bounds.yMin && velocity.y < 0f || position.y >= bounds.yMax && velocity.y > 0f)
			{
				velocity.y = 0f;
			}

			return velocity;
		}

		public static float DistanceToEdge(Vector2 position)
		{
			Rect bounds = Area;
			return Mathf.Min(
				Mathf.Min(position.x - bounds.xMin, bounds.xMax - position.x),
				Mathf.Min(position.y - bounds.yMin, bounds.yMax - position.y));
		}
	}
}
