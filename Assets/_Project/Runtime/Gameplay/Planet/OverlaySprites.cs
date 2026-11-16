using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    [NoAutoStaticsCleanup]
    public static class OverlaySprites
    {
        private const int Size = 128;
        private const float RingThicknessPixels = 5f;
        private const float ThickRingThicknessPixels = 16f;

        private static Sprite _ring;
        private static Sprite _thickRing;
        private static Sprite _dot;

        public static Sprite Ring
        {
            get
            {
                if (_ring == null)
                {
                    _ring = BuildRing(RingThicknessPixels);
                }

                return _ring;
            }
        }

        public static Sprite ThickRing
        {
            get
            {
                if (_thickRing == null)
                {
                    _thickRing = BuildRing(ThickRingThicknessPixels);
                }

                return _thickRing;
            }
        }

        public static Sprite Dot
        {
            get
            {
                if (_dot == null)
                {
                    _dot = BuildDot();
                }

                return _dot;
            }
        }

        private static Sprite BuildDot()
        {
            float radius = Size * 0.5f - 1f;
            return BuildSprite(distance =>
            {
                float coverage = Mathf.Clamp01(radius - distance + 0.5f);
                float soft = Mathf.Clamp01(1f - distance / radius);
                return (byte)Mathf.RoundToInt(coverage * Mathf.Lerp(0.35f, 1f, soft) * 255f);
            });
        }

        private static Sprite BuildRing(float thickness)
        {
            float outer = Size * 0.5f - 1f;
            float inner = outer - thickness;
            return BuildSprite(distance =>
            {
                float coverage = Mathf.Clamp01(outer - distance + 0.5f) * Mathf.Clamp01(distance - inner + 0.5f);
                return (byte)Mathf.RoundToInt(coverage * 255f);
            });
        }

        private static Sprite BuildSprite(Func<float, byte> alphaAtDistance)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[Size * Size];
            float center = (Size - 1) * 0.5f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    pixels[y * Size + x] = new Color32(255, 255, 255, alphaAtDistance(distance));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
