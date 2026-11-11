using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO
{
    public static class UiPointerBlocker
    {
        private static readonly List<UIDocument> Documents = new();

        public static void Register(UIDocument document)
        {
            if (document != null && !Documents.Contains(document))
            {
                Documents.Add(document);
            }
        }

        public static void Unregister(UIDocument document)
        {
            Documents.Remove(document);
        }

        public static bool IsOverUi(Vector2 screenPosition)
        {
            for (int index = Documents.Count - 1; index >= 0; index--)
            {
                UIDocument document = Documents[index];
                if (document == null)
                {
                    Documents.RemoveAt(index);
                    continue;
                }

                VisualElement root = document.rootVisualElement;
                IPanel panel = root?.panel;
                if (panel == null)
                {
                    continue;
                }

                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(
                    panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                VisualElement picked = panel.Pick(panelPosition);
                if (picked != null && picked != root)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
