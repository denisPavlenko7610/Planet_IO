using System;
using System.IO;
using UnityEngine;

namespace PlanetIO
{
	public enum FoodMessage : byte { Request = 1, Begin, Chunk, End, Delta, Effects }

	public sealed class FoodPacketWriter
	{
		public const int MaximumBytes = 1024;
		public const int RecordsPerPacket = 32;
		public readonly byte[] Buffer = new byte[MaximumBytes];
		public int Length { get; private set; }
		public void Reset(FoodMessage type, ulong epoch, ulong revision)
		{
			Length = 0;
			Byte(1); Byte((byte)type); UInt64(epoch); UInt64(revision);
		}
		public void Byte(byte value) { Buffer[Length++] = value; }
		public void UInt32(uint value) { for (int i = 0; i < 4; i++) Byte((byte)(value >> (8 * i))); }
		public void UInt64(ulong value) { for (int i = 0; i < 8; i++) Byte((byte)(value >> (8 * i))); }
		public void Float(float value) => UInt32((uint)BitConverter.SingleToInt32Bits(value));
		public void Record(FoodRecord record)
		{
			UInt64(record.Id); Float(record.Position.x); Float(record.Position.y);
			Float(record.Capacity); Float(record.ValueMultiplier); Byte(record.Flavor); Byte(record.Active ? (byte)1 : (byte)0);
		}
		public static uint Hash(uint hash, byte[] bytes, int start, int length)
		{
			for (int index = start; index < start + length; index++) hash = unchecked((hash ^ bytes[index]) * 16777619);
			return hash;
		}
	}

	public ref struct FoodPacketReader
	{
		private ReadOnlySpan<byte> _bytes;
		public int Position { get; private set; }
		public FoodPacketReader(ReadOnlySpan<byte> bytes) { _bytes = bytes; Position = 0; }
		public int Remaining => _bytes.Length - Position;
		public byte Byte() { if (Remaining < 1) throw new InvalidDataException("Truncated food packet."); return _bytes[Position++]; }
		public uint UInt32() { uint value = 0; for (int i = 0; i < 4; i++) value |= (uint)Byte() << (i * 8); return value; }
		public ulong UInt64() { ulong value = 0; for (int i = 0; i < 8; i++) value |= (ulong)Byte() << (i * 8); return value; }
		public float Float()
		{
			float value = BitConverter.Int32BitsToSingle((int)UInt32());
			if (!float.IsFinite(value)) throw new InvalidDataException("Non-finite food value.");
			return value;
		}
	}

	public readonly struct FoodEatEvent
	{
		public readonly Vector2 Position;
		public readonly float Size;
		public readonly byte Flavor;
		public readonly bool Golden;
		public FoodEatEvent(Vector2 position, float size, byte flavor, bool golden)
		{ Position = position; Size = size; Flavor = flavor; Golden = golden; }
	}

	public sealed class FoodReplica
	{
		private struct Update
		{
			public ulong Id;
			public Vector2 Position;
			public float Capacity, Value;
			public byte Flavor;
			public bool Active;
		}
		private readonly Update[] _updates = new Update[FoodPacketWriter.RecordsPerPacket];
		private readonly FoodEatEvent[] _effects = new FoodEatEvent[FoodPacketWriter.RecordsPerPacket];
		private FoodWorld _staging;
		private ulong _snapshotId, _stagingEpoch, _baseline;
		private int _expected, _received;
		private uint _hash;
		public FoodWorld World { get; private set; } = new(false, WorldBounds.Area);
		public ulong Epoch { get; private set; }
		public ulong Revision { get; private set; }
		public uint SnapshotProgress { get; private set; }
		public bool Ready { get; private set; }
		public bool NeedsSnapshot { get; private set; } = true;
		public event Action<FoodEatEvent> Eaten;

		public void Receive(byte[] bytes, int length)
		{
			try { Read(bytes, length); }
			catch (Exception error) when (error is InvalidDataException || error is OverflowException || error is ArgumentException)
			{ NeedsSnapshot = true; _staging = null; }
		}

		private void Read(byte[] bytes, int length)
		{
			if (length > FoodPacketWriter.MaximumBytes) throw new InvalidDataException("Food packet too large.");
			var reader = new FoodPacketReader(new ReadOnlySpan<byte>(bytes, 0, length));
			if (reader.Byte() != 1) throw new InvalidDataException("Unsupported food protocol.");
			FoodMessage type = (FoodMessage)reader.Byte();
			ulong epoch = reader.UInt64(), revision = reader.UInt64();
			if (epoch < Epoch) return;
			if (type == FoodMessage.Begin)
			{
				ulong token = reader.UInt64();
				int count = checked((int)reader.UInt32());
				if (reader.Remaining != 0 || count < 0 || count > FoodWorld.MaximumRecords) throw new InvalidDataException("Invalid food snapshot count.");
				if (epoch == Epoch && (revision < Revision || token <= _snapshotId)) return;
				_stagingEpoch = epoch; _baseline = revision; _snapshotId = token;
				_expected = count; _received = 0; _hash = 2166136261;
				_staging = new FoodWorld(false, WorldBounds.Area);
				NeedsSnapshot = true;
				SnapshotProgress++;

				return;
			}
			if (type == FoodMessage.Chunk || type == FoodMessage.End)
			{
				ulong token = reader.UInt64();
				if (_staging == null || epoch != _stagingEpoch || revision != _baseline || token != _snapshotId) return;
				if (type == FoodMessage.End)
				{
					uint hash = reader.UInt32();
					if (reader.Remaining != 0 || hash != _hash || _received != _expected || _staging.ActiveCount != _expected)
						throw new InvalidDataException("Incomplete food snapshot.");
					World = _staging; _staging = null; Epoch = epoch; Revision = revision; Ready = true; NeedsSnapshot = false;
					return;
				}
				int offset = checked((int)reader.UInt32());
				if (offset != _received) throw new InvalidDataException("Food snapshot chunk gap.");
				int count = reader.Byte();
				int start = reader.Position;
				ReadRecords(ref reader, count);
				if (_received + count > _expected) throw new InvalidDataException("Excess snapshot records.");
				for (int i = 0; i < count; i++) { if (!_updates[i].Active) throw new InvalidDataException("Inactive snapshot record."); Apply(_staging, _updates[i]); }
				_hash = FoodPacketWriter.Hash(_hash, bytes, start, length - start);
				_received += count;
				SnapshotProgress++;

				return;
			}
			if (type != FoodMessage.Delta && type != FoodMessage.Effects) throw new InvalidDataException("Unknown food message.");
			if (!Ready || epoch != Epoch) { NeedsSnapshot = true; return; }
			if (revision <= Revision) return;
			if (revision != Revision + 1) { NeedsSnapshot = true; return; }
			if (NeedsSnapshot) return;
			int items = reader.Byte();
			if (type == FoodMessage.Delta)
			{
				ReadRecords(ref reader, items);
				for (int i = 0; i < items; i++) Apply(World, _updates[i]);
			}
			else
			{
				if (items > _effects.Length) throw new InvalidDataException("Excess eat effects.");
				for (int i = 0; i < items; i++)
				{
					Vector2 position = new(reader.Float(), reader.Float());
					float size = reader.Float(); byte flavor = reader.Byte(), golden = reader.Byte();
					if (size <= 0 || golden > 1) throw new InvalidDataException("Invalid eat effect.");
					_effects[i] = new FoodEatEvent(position, size, flavor, golden != 0);
				}
				if (reader.Remaining != 0) throw new InvalidDataException("Trailing food packet data.");
				for (int i = 0; i < items; i++) Eaten?.Invoke(_effects[i]);
			}
			Revision = revision;
		}

		private void ReadRecords(ref FoodPacketReader reader, int count)
		{
			if (count > _updates.Length || reader.Remaining != count * 26) throw new InvalidDataException("Invalid food record count.");
			for (int i = 0; i < count; i++)
			{
				ulong id = reader.UInt64(); Vector2 position = new(reader.Float(), reader.Float());
				float capacity = reader.Float(), value = reader.Float(); byte flavor = reader.Byte(), active = reader.Byte();
				if (!FoodWorld.IsValidId(id) ||
					capacity <= 0 || capacity > 10f || value < 1f || active > 1 || (position.x < WorldBounds.Area.xMin || position.x > WorldBounds.Area.xMax || position.y < WorldBounds.Area.yMin || position.y > WorldBounds.Area.yMax))
					throw new InvalidDataException("Invalid food record.");
				_updates[i] = new Update { Id = id, Position = position, Capacity = capacity, Value = value, Flavor = flavor, Active = active != 0 };
			}
		}
		private static void Apply(FoodWorld world, Update update) => world.ApplyVisual(update.Id, update.Position, update.Capacity, update.Value, update.Flavor, update.Active);
	}
}
