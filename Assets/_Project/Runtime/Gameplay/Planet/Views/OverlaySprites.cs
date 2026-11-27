using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    [NoAutoStaticsCleanup]
    public static class OverlaySprites
    {
        private const int Size = 128;
		private const int RingSize = 512;
        private const float RingThicknessPixels = 5f;
        private const float ThickRingThicknessPixels = 16f;

        private static Sprite _ring;
        private static Sprite _thickRing;
        private static Sprite _dot;
		private static Sprite _chevron;

		public static Sprite Chevron
		{
			get
			{
				if (_chevron == null)
				{
					_chevron = BuildChevron();
				}

				return _chevron;
			}
		}

		private static Sprite BuildChevron()
		{
			Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
			{
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Bilinear,
				hideFlags = HideFlags.HideAndDontSave
			};
			Color32[] pixels = new Color32[Size * Size];
			Vector2 tip = new(92f, 63.5f);
			Vector2 upper = new(36f, 116f);
			Vector2 lower = new(36f, 11f);
			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					Vector2 point = new(x, y);
					float distance = Mathf.Min(SegmentDistance(point, upper, tip), SegmentDistance(point, lower, tip));
					float stroke = Mathf.Clamp01(6f - distance);
					float outline = Mathf.Clamp01(9f - distance);
					float shade = Mathf.Lerp(0.08f, 1f, stroke);
					pixels[y * Size + x] = new Color(shade, shade, shade, Mathf.Lerp(outline * 0.6f, 1f, stroke));
				}
			}
			texture.SetPixels32(pixels);
			texture.Apply(false, true);
			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
			sprite.hideFlags = HideFlags.HideAndDontSave;
			return sprite;
		}

		private static float SegmentDistance(Vector2 point, Vector2 start, Vector2 end)
		{
			Vector2 segment = end - start;
			float along = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
			return Vector2.Distance(point, start + segment * along);
		}

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
				}
			);
        }

		private static Sprite BuildRing(float thickness)
		{
			const float resolutionScale = (float)RingSize / Size;
			const float outer = (Size * 0.5f - 1f) * resolutionScale;
			const float edgeWidth = 3f;
			float inner = outer - thickness * resolutionScale;

			return BuildSprite(
				distance =>
				{
					float outerCoverage = Mathf.Clamp01((outer - distance) / edgeWidth + 0.5f);
					float innerCoverage = Mathf.Clamp01((distance - inner) / edgeWidth + 0.5f);
					float coverage = outerCoverage * innerCoverage;

					return (byte)Mathf.RoundToInt(coverage * 255f);
				},
				RingSize,
				mipmaps: true
			);
		}

		private static Sprite BuildSprite(Func<float, byte> alphaAtDistance, int size = Size, bool mipmaps = false)
		{
			Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, mipmaps)
			{
				wrapMode = TextureWrapMode.Clamp,
				filterMode = mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear,
				hideFlags = HideFlags.HideAndDontSave
			};

			Color32[] pixels = new Color32[size * size];
			float center = (size - 1) * 0.5f;

			for (int y = 0; y < size; y++)
			{
				for (int x = 0; x < size; x++)
				{
					float distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
					pixels[y * size + x] = new Color32(255, 255, 255, alphaAtDistance(distance));
				}
			}

			texture.SetPixels32(pixels);
			texture.Apply(mipmaps, true);

			SpriteMeshType meshType = mipmaps ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
			Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size, 0, meshType);
			sprite.hideFlags = HideFlags.HideAndDontSave;

			return sprite;
		}
    }
}
