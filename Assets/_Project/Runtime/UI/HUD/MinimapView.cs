using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Hud
{
    public readonly struct MinimapBlip
    {
        public readonly Vector2 NormalizedPosition;
        public readonly Color Color;

        public MinimapBlip(Vector2 normalizedPosition, Color color)
        {
            NormalizedPosition = normalizedPosition;
            Color = color;
        }
    }

    public sealed class MinimapView : MonoBehaviour
    {
        private const float BlipSize = 7f;

        private readonly List<Image> _blips = new();
        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();

            Image background = gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;
        }

        public void UpdateBlips(IReadOnlyList<MinimapBlip> blips)
        {
            while (_blips.Count < blips.Count)
            {
                GameObject blipObject = new($"Blip_{_blips.Count}", typeof(RectTransform));
                blipObject.transform.SetParent(transform, false);

                Image blip = blipObject.AddComponent<Image>();
                blip.raycastTarget = false;
                blip.rectTransform.sizeDelta = new Vector2(BlipSize, BlipSize);

                _blips.Add(blip);
            }

            for (int index = 0; index < _blips.Count; index++)
            {
                Image blip = _blips[index];
                if (index >= blips.Count)
                {
                    blip.gameObject.SetActive(false);
                    continue;
                }

                MinimapBlip data = blips[index];
                blip.gameObject.SetActive(true);
                blip.color = data.Color;
                blip.rectTransform.anchoredPosition =
                    (data.NormalizedPosition - Vector2.one * 0.5f) * (Vector2)_rect.rect.size;
            }
        }
    }
}
