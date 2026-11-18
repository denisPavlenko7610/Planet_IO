using UnityEngine.UIElements;

namespace PlanetIO.UI
{
    public static class VisualElementExtensions
    {
        public static void SetDisplayed(this VisualElement element, bool displayed)
        {
            element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
