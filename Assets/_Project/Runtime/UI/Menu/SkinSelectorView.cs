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
        void ShowAdStatus(string message, bool isError);
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
        private IReadOnlyList<Sprite> _sprites;
        private VisualElement _profileSkin;

        public event Action<int> SkinClicked;

        public void Build(IReadOnlyList<Sprite> sprites, string lockedLabel)
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            VisualElement container = root.Q<VisualElement>("skins");
            _profileSkin = root.Q<VisualElement>("profile-skin");
            _sprites = sprites;
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

        public void ShowAdStatus(string message, bool isError)
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            ApplyStatus(root.Q<Label>("status-text"), message, isError);
            ApplyStatus(root.Q<Label>("customize-status"), message, isError);
        }

        private static void ApplyStatus(Label status, string message, bool isError)
        {
            if (status == null)
            {
                return;
            }

            status.text = message;
            status.EnableInClassList("status--error", isError);
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

            if (selected && unlocked && _profileSkin != null && _sprites != null && index < _sprites.Count)
            {
                _profileSkin.style.backgroundImage = new StyleBackground(_sprites[index]);
            }
        }
    }
}
