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

        private readonly List<VisualElement> _blips = new();
        private readonly List<BlipState> _states = new();
        private VisualElement _map;

        private void OnEnable()
        {
            _map = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("minimap");
            _blips.Clear();
            _states.Clear();
			_map.Clear();
			_map.style.height = FallbackSize * Constants.WorldBounds.height / Constants.WorldBounds.width;
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
			if (!float.IsFinite(size.x) || !float.IsFinite(size.y) || size.x <= 0f || size.y <= 0f)
            {
				size = new Vector2(FallbackSize, FallbackSize * Constants.WorldBounds.height / Constants.WorldBounds.width);
            }

            while (_states.Count < _blips.Count)
            {
                _states.Add(default);
            }

            for (int index = 0; index < _blips.Count; index++)
            {
                VisualElement blip = _blips[index];
                BlipState state = _states[index];
                if (index >= blips.Count)
                {
                    if (state.Visible)
                    {
                        blip.style.display = DisplayStyle.None;
                        state.Visible = false;
                        _states[index] = state;
                    }

                    continue;
                }

                MinimapBlip data = blips[index];
				float blipSize = data.IsLeader || data.IsLocal ? LeaderBlipSize : BlipSize;
				float left = Mathf.Clamp(data.NormalizedPosition.x * size.x - blipSize * 0.5f, 0f, Mathf.Max(0f, size.x - blipSize));
				float top = Mathf.Clamp((1f - data.NormalizedPosition.y) * size.y - blipSize * 0.5f, 0f, Mathf.Max(0f, size.y - blipSize));

				blip.EnableInClassList("minimap-blip--local", data.IsLocal);
                if (!state.Visible)
                {
                    blip.style.display = DisplayStyle.Flex;
                    state.Visible = true;
                }

				if (!state.Initialized || state.Size != blipSize || state.IsEvent != data.IsEvent)
                {
                    blip.style.width = blipSize;
                    blip.style.height = blipSize;
					blip.style.borderTopLeftRadius = data.IsEvent ? 0f : blipSize * 0.5f;
					blip.style.borderTopRightRadius = data.IsEvent ? 0f : blipSize * 0.5f;
					blip.style.borderBottomLeftRadius = data.IsEvent ? 0f : blipSize * 0.5f;
					blip.style.borderBottomRightRadius = data.IsEvent ? 0f : blipSize * 0.5f;
					state.IsEvent = data.IsEvent;
                    state.Size = blipSize;
                }

                if (!state.Initialized || state.Color != data.Color)
                {
                    blip.style.backgroundColor = data.Color;
                    state.Color = data.Color;
                }

				if (!state.Initialized || !float.IsFinite(state.Left) || Mathf.Abs(state.Left - left) >= PositionTolerance)
                {
                    blip.style.left = left;
                    state.Left = left;
                }

				if (!state.Initialized || !float.IsFinite(state.Top) || Mathf.Abs(state.Top - top) >= PositionTolerance)
                {
                    blip.style.top = top;
                    state.Top = top;
                }

                state.Initialized = true;
                _states[index] = state;
            }
        }

        private struct BlipState
        {
			public bool IsEvent;
            public bool Initialized;
            public bool Visible;
            public float Size;
            public float Left;
            public float Top;
            public Color Color;
        }
    }
}
