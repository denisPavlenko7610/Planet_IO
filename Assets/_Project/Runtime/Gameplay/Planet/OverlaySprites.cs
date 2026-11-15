using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    [NoAutoStaticsCleanup]
    public static class OverlaySprites
    {
        private const int Size = 128;
        private const float RingThicknessPixels = 5f;

        private static Sprite _ring;
        private static Sprite _dot;

        public static Sprite Ring
        {
            get
            {
                if (_ring == null)
                {
                    _ring = BuildRing();
                }

                return _ring;
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
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[Size * Size];
            float center = (Size - 1) * 0.5f;
            float radius = Size * 0.5f - 1f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float coverage = Mathf.Clamp01(radius - distance + 0.5f);
                    float soft = Mathf.Clamp01(1f - distance / radius);
                    byte alpha = (byte)Mathf.RoundToInt(coverage * Mathf.Lerp(0.35f, 1f, soft) * 255f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite BuildRing()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[Size * Size];
            float center = (Size - 1) * 0.5f;
            float outer = Size * 0.5f - 1f;
            float inner = outer - RingThicknessPixels;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float coverage = Mathf.Clamp01(outer - distance + 0.5f) * Mathf.Clamp01(distance - inner + 0.5f);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(coverage * 255f));
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
