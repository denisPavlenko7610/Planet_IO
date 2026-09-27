using UnityEngine;

namespace PlanetIO
{
    public static class WorldBounds
    {
        public static Vector2 KeepInside(Vector2 position, Vector2 velocity)
        {
            Rect bounds = Constants.WorldBounds;
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
            Rect bounds = Constants.WorldBounds;
            return Mathf.Min(
                Mathf.Min(position.x - bounds.xMin, bounds.xMax - position.x),
                Mathf.Min(position.y - bounds.yMin, bounds.yMax - position.y));
        }
    }
}
