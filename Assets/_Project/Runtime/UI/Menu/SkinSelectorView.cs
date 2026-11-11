using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Menu
{
    public interface ISkinSelectorView
    {
        event Action<int> SkinClicked;

        void Build(IReadOnlyList<Sprite> sprites, string lockedLabel);
        void SetLockedLabel(string lockedLabel);
        void SetState(int index, bool unlocked, bool selected);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class SkinSelectorView : MonoBehaviour, ISkinSelectorView
    {
        private const string SkinClass = "skin";
        private const string SelectedClass = "skin--selected";
        private const string LockedClass = "skin--locked";
        private const string BadgeClass = "skin__badge";

        private readonly List<(VisualElement Planet, Label Badge)> _cells = new();

        public event Action<int> SkinClicked;

        public void Build(IReadOnlyList<Sprite> sprites, string lockedLabel)
        {
            VisualElement container = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("skins");
            container.Clear();
            _cells.Clear();

            for (int index = 0; index < sprites.Count; index++)
            {
                int skinIndex = index;

                VisualElement planet = new();
                planet.AddToClassList(SkinClass);
                planet.style.backgroundImage = new StyleBackground(sprites[index]);
                planet.RegisterCallback<ClickEvent>(_ => SkinClicked?.Invoke(skinIndex));

                Label badge = new(lockedLabel);
                badge.AddToClassList(BadgeClass);
                badge.pickingMode = PickingMode.Ignore;
                planet.Add(badge);

                container.Add(planet);
                _cells.Add((planet, badge));
            }
        }

        public void SetLockedLabel(string lockedLabel)
        {
            foreach ((_, Label badge) in _cells)
            {
                badge.text = lockedLabel;
            }
        }

        public void SetState(int index, bool unlocked, bool selected)
        {
            if (index < 0 || index >= _cells.Count)
            {
                return;
            }

            (VisualElement planet, Label badge) = _cells[index];
            planet.EnableInClassList(LockedClass, !unlocked);
            planet.EnableInClassList(SelectedClass, selected);
            badge.style.display = unlocked ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
