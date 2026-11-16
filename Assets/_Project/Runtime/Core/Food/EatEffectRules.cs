using UnityEngine;

namespace PlanetIO
{
    public static class EatEffectRules
    {
        public const float RelevanceHalfWidth = 75f;
        public const float RelevanceHalfHeight = 32f;

        public static bool IsRelevant(Vector2 effectPosition, Vector2 viewerPosition)
        {
            return Mathf.Abs(effectPosition.x - viewerPosition.x) <= RelevanceHalfWidth &&
                   Mathf.Abs(effectPosition.y - viewerPosition.y) <= RelevanceHalfHeight;
        }
    }
}
