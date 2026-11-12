using UnityEngine;

namespace PlanetIO
{
    public interface IFoodFieldGuide
    {
        bool TryGetFieldTarget(Vector2 from, float minimumDistance, out Vector2 target);
    }
}
