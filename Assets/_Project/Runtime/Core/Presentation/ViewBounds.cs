using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    public static class ViewCullRules
    {
        public static bool Intersects(Rect view, Vector2 center, float radius)
        {
            return center.x + radius >= view.xMin &&
                   center.x - radius <= view.xMax &&
                   center.y + radius >= view.yMin &&
                   center.y - radius <= view.yMax;
        }
    }

    [AutoStaticsCleanup]
    public static partial class ViewBounds
    {
        public const float Margin = 2f;

        private static Rect _rect;
        private static bool _known;

        public static void Set(Rect rect)
        {
            _rect = rect;
            _known = true;
        }

        public static void Clear()
        {
            _known = false;
        }

        public static bool IsVisible(Vector2 center, float radius)
        {
            return !_known || ViewCullRules.Intersects(_rect, center, radius + Margin);
        }
    }
}
