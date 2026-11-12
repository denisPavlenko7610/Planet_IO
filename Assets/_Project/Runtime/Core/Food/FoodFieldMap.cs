using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public sealed class FoodFieldMap
    {
        public const int NoCell = -1;

        private const int PlacementAttempts = 16;
        private const float FieldSpacing = 0.8f;
        private const float TargetSpread = 0.3f;
        private const float DistanceJitter = 0.4f;

        private sealed class Field
        {
            public Vector2 Center;
            public float Radius;
            public bool IsPermanent;
        }

        private readonly Rect _bounds;
        private readonly FoodFieldSettings _settings;
        private readonly System.Random _random;
        private readonly int _columns;
        private readonly int _rows;
        private readonly float _cellWidth;
        private readonly float _cellHeight;
        private readonly int _totalFood;
        private readonly List<Field> _fields = new();
        private readonly float[] _targets;
        private readonly float[] _dither;
        private readonly float[] _remainders;
        private readonly float[] _sortKeys;
        private readonly int[] _quotas;
        private readonly int[] _order;
        private readonly int[] _counts;
        private readonly float[] _scratch;
        private readonly Vector2[] _clumpAnchors;
        private readonly int[] _clumpRemaining;

        private int _placed;

        public FoodFieldMap(Rect bounds, int totalFood, FoodFieldSettings settings, int seed)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _bounds = bounds;
            _totalFood = Mathf.Max(0, totalFood);
            _random = new System.Random(seed);
            _columns = Mathf.Max(1, Mathf.CeilToInt(bounds.width / settings.CellSize));
            _rows = Mathf.Max(1, Mathf.CeilToInt(bounds.height / settings.CellSize));
            _cellWidth = bounds.width / _columns;
            _cellHeight = bounds.height / _rows;

            int cellCount = _columns * _rows;
            _targets = new float[cellCount];
            _dither = new float[cellCount];
            _remainders = new float[cellCount];
            _sortKeys = new float[cellCount];
            _quotas = new int[cellCount];
            _order = new int[cellCount];
            _counts = new int[cellCount];
            _scratch = new float[cellCount];
            _clumpAnchors = new Vector2[cellCount];
            _clumpRemaining = new int[cellCount];

            for (int cell = 0; cell < cellCount; cell++)
            {
                _dither[cell] = NextFloat();
            }

            AddHomeField();
            for (int index = 0; index < settings.FieldCount; index++)
            {
                _fields.Add(CreateField(null));
            }

            RebuildTargets();
        }

        public int CellCount => _targets.Length;
        public int PlacedCount => _placed;
        public int FieldCount => _fields.Count;

        public float GetTarget(int cell) => _targets[cell];

        public int GetQuota(int cell) => _quotas[cell];

        public int GetCount(int cell) => _counts[cell];

        public Vector2 GetFieldCenter(int fieldIndex) => _fields[fieldIndex].Center;

        public float GetFieldRadius(int fieldIndex) => _fields[fieldIndex].Radius;

        public int CellOf(Vector2 position)
        {
            int column = Mathf.Clamp(Mathf.FloorToInt((position.x - _bounds.xMin) / _cellWidth), 0, _columns - 1);
            int row = Mathf.Clamp(Mathf.FloorToInt((position.y - _bounds.yMin) / _cellHeight), 0, _rows - 1);
            return row * _columns + column;
        }

        public int Reserve(int sourceCell)
        {
            int cell = PickCell(sourceCell);
            _counts[cell]++;
            _placed++;
            return cell;
        }

        public void Release(int cell)
        {
            if (cell < 0 || cell >= _counts.Length || _counts[cell] <= 0)
            {
                return;
            }

            _counts[cell]--;
            _placed--;
        }

        public Vector2 SamplePosition(int cell)
        {
            if (_clumpRemaining[cell] <= 0)
            {
                _clumpAnchors[cell] = RandomPointInCell(cell);
                Vector2Int clumpSize = _settings.ClumpSizeRange;
                _clumpRemaining[cell] = _random.Next(clumpSize.x, clumpSize.y + 1);
            }

            _clumpRemaining[cell]--;
            Vector2 position = _clumpAnchors[cell] + RandomInsideCircle() * _settings.ClumpRadius;
            return ClampToBounds(position);
        }

        public bool RelocateField()
        {
            int candidateCount = 0;
            for (int index = 0; index < _fields.Count; index++)
            {
                if (!_fields[index].IsPermanent)
                {
                    candidateCount++;
                }
            }

            if (candidateCount == 0)
            {
                return false;
            }

            int pick = _random.Next(candidateCount);
            for (int index = 0; index < _fields.Count; index++)
            {
                if (_fields[index].IsPermanent)
                {
                    continue;
                }

                if (pick-- == 0)
                {
                    _fields[index] = CreateField(_fields[index]);
                    break;
                }
            }

            RebuildTargets();
            return true;
        }

        public bool TryGetFieldTarget(Vector2 from, float minimumDistance, out Vector2 target)
        {
            target = default;
            float bestScore = float.PositiveInfinity;
            Field best = null;

            for (int index = 0; index < _fields.Count; index++)
            {
                Field field = _fields[index];
                float distance = Vector2.Distance(from, field.Center);
                if (distance < minimumDistance)
                {
                    continue;
                }

                float score = distance * (1f - DistanceJitter + DistanceJitter * 2f * NextFloat());
                if (score < bestScore)
                {
                    bestScore = score;
                    best = field;
                }
            }

            if (best == null)
            {
                return false;
            }

            target = ClampToBounds(best.Center + RandomInsideCircle() * (best.Radius * TargetSpread));
            return true;
        }

        private void AddHomeField()
        {
            _fields.Add(new Field
            {
                Center = ClampToBounds(_settings.HomeCenter),
                Radius = _settings.HomeRadius,
                IsPermanent = true
            });
        }

        private Field CreateField(Field replacing)
        {
            Vector2 radiusRange = _settings.FieldRadiusRange;
            float radius = Mathf.Lerp(radiusRange.x, radiusRange.y, NextFloat());
            float insetX = Mathf.Min(radius * 0.5f, _bounds.width * 0.25f);
            float insetY = Mathf.Min(radius * 0.5f, _bounds.height * 0.25f);
            Vector2 center = default;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                center = new Vector2(
                    Mathf.Lerp(_bounds.xMin + insetX, _bounds.xMax - insetX, NextFloat()),
                    Mathf.Lerp(_bounds.yMin + insetY, _bounds.yMax - insetY, NextFloat()));

                if (!OverlapsExistingField(center, radius, replacing))
                {
                    break;
                }
            }

            return new Field { Center = center, Radius = radius, IsPermanent = false };
        }

        private bool OverlapsExistingField(Vector2 center, float radius, Field ignored)
        {
            for (int index = 0; index < _fields.Count; index++)
            {
                Field other = _fields[index];
                if (ReferenceEquals(other, ignored))
                {
                    continue;
                }

                if (Vector2.Distance(center, other.Center) < (radius + other.Radius) * FieldSpacing)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildTargets()
        {
            float fieldSum = 0f;

            for (int cell = 0; cell < _targets.Length; cell++)
            {
                Vector2 center = CellCenter(cell);
                float weight = 0f;

                for (int index = 0; index < _fields.Count; index++)
                {
                    Field field = _fields[index];
                    float normalized = Vector2.Distance(center, field.Center) / field.Radius;
                    if (normalized >= 1f)
                    {
                        continue;
                    }

                    float falloff = 1f - normalized * normalized;
                    weight += falloff * falloff;
                }

                _scratch[cell] = weight;
                fieldSum += weight;
            }

            float ambientShare = _settings.AmbientShare;
            float ambientPerCell = _totalFood * ambientShare / _targets.Length;
            float fieldTotal = _totalFood * (1f - ambientShare);

            for (int cell = 0; cell < _targets.Length; cell++)
            {
                float fieldPart = fieldSum > 0f
                    ? fieldTotal * _scratch[cell] / fieldSum
                    : fieldTotal / _targets.Length;
                _targets[cell] = ambientPerCell + fieldPart;
            }

            RebuildQuotas();
        }

        private void RebuildQuotas()
        {
            int sum = 0;

            for (int cell = 0; cell < _targets.Length; cell++)
            {
                float adjusted = _targets[cell] + _dither[cell];
                int quota = Mathf.FloorToInt(adjusted);
                _quotas[cell] = quota;
                _remainders[cell] = adjusted - quota;
                sum += quota;
            }

            int difference = _totalFood - sum;
            if (difference != 0)
            {
                BalanceQuotas(difference);
            }
        }

        private void BalanceQuotas(int difference)
        {
            for (int cell = 0; cell < _order.Length; cell++)
            {
                _order[cell] = cell;
                _sortKeys[cell] = _remainders[cell];
            }

            Array.Sort(_sortKeys, _order);

            int cursor = 0;
            int guard = _order.Length * 4 + Mathf.Abs(difference);

            while (difference != 0 && guard-- > 0)
            {
                if (difference > 0)
                {
                    _quotas[_order[_order.Length - 1 - cursor % _order.Length]]++;
                    difference--;
                }
                else
                {
                    int cell = _order[cursor % _order.Length];
                    if (_quotas[cell] > 0)
                    {
                        _quotas[cell]--;
                        difference++;
                    }
                }

                cursor++;
            }
        }

        private int PickCell(int sourceCell)
        {
            bool avoidSource = sourceCell != NoCell && NextFloat() >= _settings.LocalRegrowChance;

            float total = FillWeights(sourceCell, avoidSource, true);
            if (total <= 0f)
            {
                total = FillWeights(sourceCell, avoidSource, false);
            }

            return total > 0f ? SelectWeighted(total) : _random.Next(_targets.Length);
        }

        private float FillWeights(int sourceCell, bool avoidSource, bool useDeficit)
        {
            float total = 0f;

            for (int cell = 0; cell < _targets.Length; cell++)
            {
                float weight = 0f;
                if (!avoidSource || !IsNear(cell, sourceCell))
                {
                    weight = useDeficit ? Mathf.Max(0, _quotas[cell] - _counts[cell]) : _targets[cell];
                }

                _scratch[cell] = weight;
                total += weight;
            }

            return total;
        }

        private int SelectWeighted(float total)
        {
            float roll = NextFloat() * total;
            int last = 0;

            for (int cell = 0; cell < _scratch.Length; cell++)
            {
                if (_scratch[cell] <= 0f)
                {
                    continue;
                }

                last = cell;
                roll -= _scratch[cell];
                if (roll <= 0f)
                {
                    return cell;
                }
            }

            return last;
        }

        private bool IsNear(int cell, int sourceCell)
        {
            int columnDelta = Mathf.Abs(cell % _columns - sourceCell % _columns);
            int rowDelta = Mathf.Abs(cell / _columns - sourceCell / _columns);
            return columnDelta <= 1 && rowDelta <= 1;
        }

        private Vector2 CellCenter(int cell)
        {
            return new Vector2(
                _bounds.xMin + (cell % _columns + 0.5f) * _cellWidth,
                _bounds.yMin + (cell / _columns + 0.5f) * _cellHeight);
        }

        private Vector2 RandomPointInCell(int cell)
        {
            return new Vector2(
                _bounds.xMin + (cell % _columns + NextFloat()) * _cellWidth,
                _bounds.yMin + (cell / _columns + NextFloat()) * _cellHeight);
        }

        private Vector2 ClampToBounds(Vector2 position)
        {
            return new Vector2(
                Mathf.Clamp(position.x, _bounds.xMin, _bounds.xMax),
                Mathf.Clamp(position.y, _bounds.yMin, _bounds.yMax));
        }

        private Vector2 RandomInsideCircle()
        {
            float angle = NextFloat() * Mathf.PI * 2f;
            float distance = Mathf.Sqrt(NextFloat());
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        private float NextFloat() => (float)_random.NextDouble();
    }
}
