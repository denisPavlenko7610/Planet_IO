using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Hud
{
    public readonly struct MinimapBlip
    {
        public readonly Vector2 NormalizedPosition;
        public readonly Color Color;
        public readonly bool IsLeader;

        public MinimapBlip(Vector2 normalizedPosition, Color color, bool isLeader = false)
        {
            NormalizedPosition = normalizedPosition;
            Color = color;
            IsLeader = isLeader;
        }
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class MinimapView : MonoBehaviour
    {
        private const float BlipSize = 7f;
        private const float LeaderBlipSize = 13f;
        private const float FallbackSize = 180f;

        private readonly List<VisualElement> _blips = new();
        private VisualElement _map;

        private void OnEnable()
        {
            _map = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("minimap");
            _blips.Clear();
            _map.Clear();
        }

        public void UpdateBlips(IReadOnlyList<MinimapBlip> blips)
        {
            if (_map == null)
            {
                return;
            }

            while (_blips.Count < blips.Count)
            {
                VisualElement blip = new();
                blip.AddToClassList("minimap-blip");
                blip.pickingMode = PickingMode.Ignore;
                _map.Add(blip);
                _blips.Add(blip);
            }

            Vector2 size = _map.contentRect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = new Vector2(FallbackSize, FallbackSize);
            }

            for (int index = 0; index < _blips.Count; index++)
            {
                VisualElement blip = _blips[index];
                if (index >= blips.Count)
                {
                    blip.style.display = DisplayStyle.None;
                    continue;
                }

                MinimapBlip data = blips[index];
                float blipSize = data.IsLeader ? LeaderBlipSize : BlipSize;
                blip.style.display = DisplayStyle.Flex;
                blip.style.backgroundColor = data.Color;
                blip.style.width = blipSize;
                blip.style.height = blipSize;
                blip.style.borderTopLeftRadius = blipSize * 0.5f;
                blip.style.borderTopRightRadius = blipSize * 0.5f;
                blip.style.borderBottomLeftRadius = blipSize * 0.5f;
                blip.style.borderBottomRightRadius = blipSize * 0.5f;
                blip.style.left = data.NormalizedPosition.x * size.x - blipSize * 0.5f;
                blip.style.top = (1f - data.NormalizedPosition.y) * size.y - blipSize * 0.5f;
            }
        }
    }
}
