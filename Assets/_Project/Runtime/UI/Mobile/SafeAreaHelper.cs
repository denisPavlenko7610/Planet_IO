using UnityEngine;

namespace PlanetIO.UI.Mobile
{
    public static class SafeAreaHelper
    {
        public static bool HasScreenChanged(
            ref Rect lastSafeArea,
            ref Vector2Int lastScreenSize)
        {
            Rect current = Screen.safeArea;
            Vector2Int screenSize = new(Screen.width, Screen.height);

            if (lastSafeArea == current && lastScreenSize == screenSize)
            {
                return false;
            }

            lastSafeArea = current;
            lastScreenSize = screenSize;
            return true;
        }
    }
}
