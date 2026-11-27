using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
	public sealed class FoodWorld
	{
		public const int MaximumRecords = 8192;
		public const float CellSize = 8f;
		private readonly FoodRecord[] _records = new FoodRecord[MaximumRecords];
		private readonly int[] _heads;
		private readonly Stack<int> _free = new();
		private readonly Rect _bounds;
		private readonly int _width;
		private readonly int _height;
		private int _used;
		private float _maximumRadius;

		public FoodWorld(bool authoritative, Rect bounds)
		{
			Authoritative = authoritative;
			_bounds = bounds;
			_width = Mathf.CeilToInt(bounds.width / CellSize) + 1;
			_height = Mathf.CeilToInt(bounds.height / CellSize) + 1;
			_heads = new int[_width * _height];
			Array.Fill(_heads, -1);
		}

		public bool Authoritative { get; }
		public float Clock { get; private set; }
		public int ActiveCount { get; private set; }
		public int UsedSlots => _used;
		public event Action<FoodRecord> Changed;
		public FoodRecord At(int index) => index >= 0 && index < _used ? _records[index] : null;

		public void Advance(float deltaTime)
		{
			RequireAuthority();
			Clock += Mathf.Max(0f, deltaTime);
		}

		public void RequireAuthority()
		{
			if (!Authoritative)
				throw new InvalidOperationException("Only the server can mutate food.");
		}

		public FoodRecord Create(Vector2 position, float capacity, byte flavor = 0)
		{
			RequireAuthority();
			int slot = _free.Count > 0 ? _free.Pop() : _used;
			if (slot >= MaximumRecords)
				throw new InvalidOperationException("Food table capacity exhausted.");

			if (slot == _used)
				_used++;
			FoodRecord record = _records[slot] ??= new FoodRecord { World = this, Slot = slot };
			if (record.Generation >= 0x7ffffffe)
				throw new InvalidOperationException("Food generation exhausted.");

			record.Generation++;
			record.Id = MakeId(slot, record.Generation);
			record.Active = true;
			record.Claimed = false;
			record.StoredPosition = position;
			record.StoredCapacity = capacity;
			record.Flavor = flavor;
			record.Nutrition = 0f;
			record.ValueMultiplier = 1f;
			record.IsDropped = false;
			record.IsCometTrail = false;
			record.DensityCell = -1;
			record.DropperId = ulong.MaxValue;
			record.ImmuneUntil = 0f;
			record.ExpiresAt = 0f;
			ActiveCount++;
			Link(record);
			Touch(record);
			return record;
		}

		public static ulong MakeId(int slot, uint generation) =>
			0x8000000000000000UL | ((ulong)generation << 32) | (uint)slot;

		public static int SlotOf(ulong id) => (int)(id & 0xffffffff);

		public static uint GenerationOf(ulong id) => (uint)((id >> 32) & 0x7fffffff);

		public static bool IsValidId(ulong id) =>
			(id >> 63) != 0 && (uint)id < MaximumRecords && GenerationOf(id) != 0;

		public FoodRecord Find(ulong id)
		{
			FoodRecord record = At(SlotOf(id));
			return record != null && record.Active && record.Id == id ? record : null;
		}

		public void Remove(FoodRecord record)
		{
			RequireAuthority();
			if (record == null || !record.Active || record.World != this)
				return;

			Unlink(record);
			record.Active = false;
			ActiveCount--;
			_free.Push(record.Slot);
			Touch(record);
		}

		public void Renew(FoodRecord record)
		{
			RequireAuthority();
			if (!record.Active)
				throw new InvalidOperationException("Cannot respawn inactive food.");
			if (record.Generation >= 0x7ffffffe) throw new InvalidOperationException("Food generation exhausted.");

			record.Generation++;
			record.Id = MakeId(record.Slot, record.Generation);
			record.Claimed = false;
			record.DropperId = ulong.MaxValue;
			record.ImmuneUntil = 0f;
			Touch(record);
		}

		public void Move(FoodRecord record, Vector2 position)
		{
			RequireAuthority();
			position.x = Mathf.Clamp(position.x, _bounds.xMin, _bounds.xMax);
			position.y = Mathf.Clamp(position.y, _bounds.yMin, _bounds.yMax);
			if (!record.Active || record.World != this || position == record.StoredPosition)
				return;

			Unlink(record);
			record.StoredPosition = position;
			Link(record);
			Touch(record);
		}

		public void Touch(FoodRecord record)
		{
			RequireAuthority();
			_maximumRadius = Mathf.Max(_maximumRadius, record.Radius);
			Changed?.Invoke(record);
		}

		public void Query(Vector2 center, float radius, List<FoodRecord> result)
		{
			result.Clear();
			float extent = Mathf.Max(0f, radius) + _maximumRadius;
			QueryRect(new Rect(center.x - extent, center.y - extent, extent * 2f, extent * 2f), result);
			for (int index = result.Count - 1; index >= 0; index--)
			{
				FoodRecord food = result[index];
				float reach = radius + food.Radius;
				if ((food.Position - center).sqrMagnitude > reach * reach)
					result.RemoveAt(index);
			}
		}

		public void QueryRect(Rect rect, List<FoodRecord> result)
		{
			result.Clear();
			int minX = X(rect.xMin), maxX = X(rect.xMax), minY = Y(rect.yMin), maxY = Y(rect.yMax);
			for (int y = minY; y <= maxY; y++)
			for (int x = minX; x <= maxX; x++)
			{
				int index = _heads[y * _width + x];
				while (index >= 0)
				{
					FoodRecord food = _records[index];
					if (food.Position.x >= rect.xMin && food.Position.x <= rect.xMax && food.Position.y >= rect.yMin && food.Position.y <= rect.yMax)
						result.Add(food);
					index = food.Next;
				}
			}
		}

		public void Configure(FoodRecord record, bool dropped, bool comet, int cell, float nutrition, float value, float lifetime = 0f)
		{
			RequireAuthority();
			record.IsDropped = dropped;
			record.IsCometTrail = comet;
			record.DensityCell = cell;
			record.Nutrition = Mathf.Max(0f, nutrition);
			record.ValueMultiplier = Mathf.Max(1f, value);
			record.ExpiresAt = lifetime > 0f ? Clock + lifetime : 0f;
			Touch(record);
		}

		public void ApplyVisual(ulong id, Vector2 position, float capacity, float value, byte flavor, bool active)
		{
			int slot = SlotOf(id);
			uint generation = GenerationOf(id);
			if (!TryPrepareRecordUpdate(slot, generation, out FoodRecord record))
			{
				return;
			}

			record.Id = id;
			record.Generation = generation;
			record.StoredPosition = position;
			record.StoredCapacity = capacity;
			record.ValueMultiplier = value;
			record.Flavor = flavor;
			record.Active = active;
			FinishRecordUpdate(record);
		}

		private bool TryPrepareRecordUpdate(int slot, uint generation, out FoodRecord record)
		{
			record = _records[slot] ??= new FoodRecord { World = this, Slot = slot };
			if (record.Generation > generation)
			{
				return false;
			}

			if (record.Active)
			{
				Unlink(record);
				ActiveCount--;
			}

			_used = Mathf.Max(_used, slot + 1);

			return true;
		}

		private void FinishRecordUpdate(FoodRecord record)
		{
			_maximumRadius = Mathf.Max(_maximumRadius, record.Radius);
			if (record.Active)
			{
				ActiveCount++;
				Link(record);
			}
		}

		private int X(float value) => Mathf.Clamp(Mathf.FloorToInt((value - _bounds.xMin) / CellSize), 0, _width - 1);
		private int Y(float value) => Mathf.Clamp(Mathf.FloorToInt((value - _bounds.yMin) / CellSize), 0, _height - 1);

		private void Link(FoodRecord record)
		{
			int cell = Y(record.Position.y) * _width + X(record.Position.x);
			record.GridCell = cell;
			record.Previous = -1;
			record.Next = _heads[cell];
			if (record.Next >= 0)
				_records[record.Next].Previous = record.Slot;
			_heads[cell] = record.Slot;
		}

		private void Unlink(FoodRecord record)
		{
			if (record.GridCell < 0)
				return;

			if (record.Previous >= 0)
				_records[record.Previous].Next = record.Next;
			else
				_heads[record.GridCell] = record.Next;
			if (record.Next >= 0)
				_records[record.Next].Previous = record.Previous;
			record.GridCell = record.Next = record.Previous = -1;
		}
	}
}