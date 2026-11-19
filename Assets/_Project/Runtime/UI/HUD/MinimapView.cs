using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Hud
{
    public readonly struct MinimapBlip
    {
        public readonly Vector2 NormalizedPosition;
        public readonly Color Color;
		public readonly bool IsLocal;
		public readonly bool IsEvent;
        public readonly bool IsLeader;

		public MinimapBlip(Vector2 normalizedPosition, Color color, bool isLeader = false, bool isLocal = false, bool isEvent = false)
        {
            NormalizedPosition = normalizedPosition;
            Color = color;
			IsLocal = isLocal;
			IsEvent = isEvent;
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
		private const float PositionTolerance = 0.25f;

		private readonly List<BlipSlot> _slots = new();
		private VisualElement _map;

		private static float FallbackHeight => FallbackSize * Constants.WorldBounds.height / Constants.WorldBounds.width;

		private void OnEnable()
		{
			_map = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("minimap");
			_slots.Clear();
			_map.Clear();
			_map.style.height = FallbackHeight;
		}

		public void UpdateBlips(IReadOnlyList<MinimapBlip> blips)
		{
			if (_map == null)
			{
				return;
			}

			while (_slots.Count < blips.Count)
			{
				_slots.Add(CreateSlot());
			}

			Vector2 size = ResolveMapSize();

			for (int index = 0; index < _slots.Count; index++)
			{
				if (index < blips.Count)
				{
					Apply(_slots[index], blips[index], size);
				}
				else
				{
					Hide(_slots[index]);
				}
			}
		}

		private BlipSlot CreateSlot()
		{
			VisualElement element = new();
			element.AddToClassList("minimap-blip");
			element.pickingMode = PickingMode.Ignore;
			_map.Add(element);

			return new BlipSlot(element);
		}

		private Vector2 ResolveMapSize()
		{
			Vector2 size = _map.contentRect.size;
			if (!float.IsFinite(size.x) || !float.IsFinite(size.y) || size.x <= 0f || size.y <= 0f)
			{
				return new Vector2(FallbackSize, FallbackHeight);
			}

			return size;
		}

		private static void Hide(BlipSlot slot)
		{
			if (!slot.Visible)
			{
				return;
			}

			slot.Element.style.display = DisplayStyle.None;
			slot.Visible = false;
		}

		private static void Apply(BlipSlot slot, MinimapBlip data, Vector2 mapSize)
		{
			VisualElement element = slot.Element;
			float blipSize = data.IsLeader || data.IsLocal ? LeaderBlipSize : BlipSize;
			float left = Mathf.Clamp(data.NormalizedPosition.x * mapSize.x - blipSize * 0.5f, 0f, Mathf.Max(0f, mapSize.x - blipSize));
			float top = Mathf.Clamp((1f - data.NormalizedPosition.y) * mapSize.y - blipSize * 0.5f, 0f, Mathf.Max(0f, mapSize.y - blipSize));

			element.EnableInClassList("minimap-blip--local", data.IsLocal);

			if (!slot.Visible)
			{
				element.style.display = DisplayStyle.Flex;
				slot.Visible = true;
			}

			if (!slot.Initialized || slot.Size != blipSize || slot.IsEvent != data.IsEvent)
			{
				float radius = data.IsEvent ? 0f : blipSize * 0.5f;
				element.style.width = blipSize;
				element.style.height = blipSize;
				element.style.borderTopLeftRadius = radius;
				element.style.borderTopRightRadius = radius;
				element.style.borderBottomLeftRadius = radius;
				element.style.borderBottomRightRadius = radius;
				slot.IsEvent = data.IsEvent;
				slot.Size = blipSize;
			}

			if (!slot.Initialized || slot.Color != data.Color)
			{
				element.style.backgroundColor = data.Color;
				slot.Color = data.Color;
			}

			if (!slot.Initialized || !float.IsFinite(slot.Left) || Mathf.Abs(slot.Left - left) >= PositionTolerance)
			{
				element.style.left = left;
				slot.Left = left;
			}

			if (!slot.Initialized || !float.IsFinite(slot.Top) || Mathf.Abs(slot.Top - top) >= PositionTolerance)
			{
				element.style.top = top;
				slot.Top = top;
			}

			slot.Initialized = true;
		}

		private sealed class BlipSlot
		{
			public readonly VisualElement Element;
			public bool Initialized;
			public bool Visible;
			public bool IsEvent;
			public float Size;
			public float Left;
			public float Top;
			public Color Color;

			public BlipSlot(VisualElement element)
			{
				Element = element;
			}
		}
	}
}
