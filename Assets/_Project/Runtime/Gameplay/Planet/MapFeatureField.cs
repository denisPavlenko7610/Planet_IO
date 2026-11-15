using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    [AutoStaticsCleanup]
    public static partial class MapFeatureField
    {
        public const int AnchorSlots = MapFeatureRules.StarCount + 1;
        public const int BlackHoleSlot = MapFeatureRules.StarCount;

        public readonly struct Anchor
        {
            public readonly Vector2 Position;
            public readonly float Radius;
            public readonly float ZoneRadius;
            public readonly float Strength;

            public Anchor(Vector2 position, float radius, float zoneRadius, float strength)
            {
                Position = position;
                Radius = radius;
                ZoneRadius = zoneRadius;
                Strength = strength;
            }
        }

        private static Anchor[] _anchors;
        private static MapDisc[] _nebulae;
        private static float _gravityScale = 1f;

        public static float GravityScale
        {
            get => _gravityScale <= 0f ? 1f : _gravityScale;
            set => _gravityScale = Mathf.Max(0.01f, value);
        }

        public static Anchor[] Anchors => _anchors ??= new Anchor[AnchorSlots];

        public static void Reset()
        {
            Array.Clear(Anchors, 0, Anchors.Length);
            _nebulae = Array.Empty<MapDisc>();
            _gravityScale = 1f;
        }

        public static void SetAnchor(int slot, Anchor anchor)
        {
            if (slot >= 0 && slot < AnchorSlots)
            {
                Anchors[slot] = anchor;
            }
        }

        public static void ClearAnchor(int slot)
        {
            if (slot >= 0 && slot < AnchorSlots)
            {
                Anchors[slot] = default;
            }
        }

        public static void SetNebulae(MapDisc[] discs)
        {
            _nebulae = discs ?? Array.Empty<MapDisc>();
        }

        public static bool IsInNebula(Vector2 point) => MapFeatureRules.InsideAny(_nebulae, point);
    }
}
