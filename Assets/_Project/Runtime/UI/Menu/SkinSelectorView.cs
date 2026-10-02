using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Menu
{
    public interface ISkinSelectorView
    {
        event Action<int> SkinClicked;

        void Build(IReadOnlyList<Sprite> sprites, string lockedLabel);
        void SetLockedLabel(string lockedLabel);
        void SetState(int index, bool unlocked, bool selected);
    }

    public sealed class SkinSelectorView : MonoBehaviour, ISkinSelectorView
    {
        private const float SelectedScale = 1.12f;
        private static readonly Color LockedTint = new(0.45f, 0.45f, 0.55f, 1f);
        private static readonly Color SelectedOutline = new(1f, 0.85f, 0.3f, 1f);
        private static readonly Color BadgeColor = new(0.15f, 0.55f, 1f, 0.95f);

        [SerializeField] private RectTransform _container;
        [SerializeField, Min(24f)] private float _cellSize = 76f;
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Sprite _badgeSprite;

        private readonly List<(Image Planet, Outline Ring, GameObject Badge, TMP_Text BadgeText)> _cells = new();

        public event Action<int> SkinClicked;

        public void Build(IReadOnlyList<Sprite> sprites, string lockedLabel)
        {
            for (int index = 0; index < sprites.Count; index++)
            {
                int skinIndex = index;
                GameObject cell = new($"Skin_{index}", typeof(RectTransform));
                cell.transform.SetParent(_container, false);
                ((RectTransform)cell.transform).sizeDelta = new Vector2(_cellSize, _cellSize);

                Image planet = cell.AddComponent<Image>();
                planet.sprite = sprites[index];
                planet.preserveAspect = true;
                Outline ring = cell.AddComponent<Outline>();
                ring.effectColor = SelectedOutline;
                ring.effectDistance = new Vector2(3f, -3f);
                ring.enabled = false;

                Button button = cell.AddComponent<Button>();
                button.targetGraphic = planet;
                button.onClick.AddListener(() => SkinClicked?.Invoke(skinIndex));

                (GameObject badge, TMP_Text badgeText) = CreateBadge(cell.transform, lockedLabel);
                _cells.Add((planet, ring, badge, badgeText));
            }
        }

        public void SetLockedLabel(string lockedLabel)
        {
            foreach ((_, _, _, TMP_Text badgeText) in _cells)
            {
                badgeText.text = lockedLabel;
            }
        }

        public void SetState(int index, bool unlocked, bool selected)
        {
            if (index < 0 || index >= _cells.Count)
            {
                return;
            }

            (Image planet, Outline ring, GameObject badge, _) = _cells[index];
            planet.color = unlocked ? Color.white : LockedTint;
            ring.enabled = selected;
            badge.SetActive(!unlocked);
            planet.transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
        }

        private (GameObject, TMP_Text) CreateBadge(Transform parent, string label)
        {
            GameObject badge = new("AdBadge", typeof(RectTransform));
            badge.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)badge.transform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(_cellSize * 0.95f, _cellSize * 0.32f);
            rect.anchoredPosition = Vector2.zero;

            Image background = badge.AddComponent<Image>();
            background.sprite = _badgeSprite;
            background.type = Image.Type.Sliced;
            background.color = BadgeColor;
            background.raycastTarget = false;

            GameObject textObject = new("Label", typeof(RectTransform));
            textObject.transform.SetParent(badge.transform, false);
            RectTransform textRect = (RectTransform)textObject.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = 20f;
            text.color = Color.white;
            text.raycastTarget = false;
            return (badge, text);
        }
    }
}
