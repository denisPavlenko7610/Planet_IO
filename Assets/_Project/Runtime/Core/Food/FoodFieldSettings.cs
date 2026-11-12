using System;
using UnityEngine;

namespace PlanetIO
{
    [Serializable]
    public sealed class FoodFieldSettings
    {
        [SerializeField, Min(2f)] private float _cellSize = 12f;
        [SerializeField, Min(0)] private int _fieldCount = 16;
        [SerializeField] private Vector2 _fieldRadiusRange = new(14f, 24f);
        [SerializeField] private Vector2 _homeCenter = Vector2.zero;
        [SerializeField, Min(1f)] private float _homeRadius = 22f;
        [SerializeField, Range(0f, 1f)] private float _ambientShare = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _localRegrowChance = 0.3f;
        [SerializeField, Min(0f)] private float _relocateInterval = 45f;
        [SerializeField] private Vector2Int _clumpSizeRange = new(3, 6);
        [SerializeField, Min(0.1f)] private float _clumpRadius = 2.2f;

        public float CellSize => Mathf.Max(2f, _cellSize);
        public int FieldCount => Mathf.Max(0, _fieldCount);
        public Vector2 HomeCenter => _homeCenter;
        public float HomeRadius => Mathf.Max(1f, _homeRadius);
        public float AmbientShare => Mathf.Clamp01(_ambientShare);
        public float LocalRegrowChance => Mathf.Clamp01(_localRegrowChance);
        public float RelocateInterval => Mathf.Max(0f, _relocateInterval);
        public float ClumpRadius => Mathf.Max(0.1f, _clumpRadius);

        public Vector2 FieldRadiusRange
        {
            get
            {
                float minimum = Mathf.Max(1f, Mathf.Min(_fieldRadiusRange.x, _fieldRadiusRange.y));
                float maximum = Mathf.Max(minimum, Mathf.Max(_fieldRadiusRange.x, _fieldRadiusRange.y));
                return new Vector2(minimum, maximum);
            }
        }

        public Vector2Int ClumpSizeRange
        {
            get
            {
                int minimum = Mathf.Max(1, Mathf.Min(_clumpSizeRange.x, _clumpSizeRange.y));
                int maximum = Mathf.Max(minimum, Mathf.Max(_clumpSizeRange.x, _clumpSizeRange.y));
                return new Vector2Int(minimum, maximum);
            }
        }
    }
}
