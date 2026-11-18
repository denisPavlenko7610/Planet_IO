using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
	public sealed class FoodWorldTests
	{
		[Test]
		public void GridMatchesBruteForceAfterMovementRemovalAndSlotReuse()
		{
			var world = new FoodWorld(true, Constants.WorldBounds);
			var random = new System.Random(42);
			var records = new List<FoodRecord>();
			var result = new List<FoodRecord>(256);
			for (int i = 0; i < 2000; i++) records.Add(world.Create(new Vector2((float)random.NextDouble() * 400f - 200f, (float)random.NextDouble() * 260f - 120f), 0.2f));
			for (int i = 0; i < 200; i++) records[i].Position = new Vector2(i % 10, i % 7);
			for (int i = 200; i < 250; i++) world.Remove(records[i]);
			for (int i = 0; i < 50; i++) world.Create(Vector2.one, 0.5f);
			for (int i = 0; i < 100; i++)
			{
				Vector2 center = new((float)random.NextDouble() * 30f - 10f, (float)random.NextDouble() * 30f - 10f);
				world.Query(center, 8f, result);
				ulong[] expected = Enumerable.Range(0, world.UsedSlots).Select(world.At).Where(food => food.Active &&
					(food.Position - center).sqrMagnitude <= (8f + food.Radius) * (8f + food.Radius)).Select(food => food.Id).OrderBy(id => id).ToArray();
				Assert.That(result.Select(food => food.Id).OrderBy(id => id), Is.EqualTo(expected));
			}
		}

		[Test]
		public void RemovedHandleCannotClaimTheReplacementAndClaimsAreExclusive()
		{
			var world = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = world.Create(Vector2.zero, 0.2f);
			ulong old = food.Id;
			Assert.That(food.TryClaim(), Is.True);
			Assert.That(food.TryClaim(), Is.False);
			world.Remove(food);
			FoodRecord next = world.Create(Vector2.one, 0.3f);
			Assert.That(world.Find(old), Is.Null);
			Assert.That(next.Id, Is.Not.EqualTo(old));
			Assert.That(next.TryClaim(), Is.True);
		}

		[Test]
		public void MigrationPreservesGenerationExpiryImmunityAndInactiveSlots()
		{
			var world = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = world.Create(Vector2.one, 0.3f);
			world.Configure(food, true, false, -1, 0.1f, 2f, 20f);
			food.SetDropper(7, 5f);
			world.Advance(2f);
			FoodRecord inactive = world.Create(Vector2.zero, 0.2f);
			ulong expired = inactive.Id;
			world.Remove(inactive);
			var restored = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord copy = restored.Restore(world.Capture(food));
			restored.Restore(world.Capture(inactive));
			restored.RebuildFreeSlots();
			Assert.That(copy.Id, Is.EqualTo(food.Id));
			Assert.That(copy.ExpiresAt, Is.EqualTo(18f));
			Assert.That(copy.IsMagnetImmuneFor(7), Is.True);
			restored.Advance(3.01f);
			Assert.That(copy.IsMagnetImmuneFor(7), Is.False);
			FoodRecord replacement = restored.Create(Vector2.zero, 0.2f);
			Assert.That(replacement.Id, Is.Not.EqualTo(expired));
			Assert.That(restored.ActiveCount, Is.EqualTo(2));
		}

		[Test]
		public void SweptPickupCatchesPelletsCrossedBetweenSamplesWithoutCollectingBesideThePath()
		{
			Assert.That(FoodPickupRules.Intersects(Vector2.zero, Vector2.right, new Vector2(0.5f, 0f), 0.1f), Is.True);
			Assert.That(FoodPickupRules.Intersects(Vector2.zero, Vector2.right, new Vector2(0.5f, 0.2f), 0.1f), Is.False);
		}

		[Test]
		public void GridIncludesTouchingFoodAndMagnetCannotMoveItOutsideTheArena()
		{
			var world = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = world.Create(new Vector2(1.1f, 0f), 0.2f);
			var buffer = new List<FoodRecord>();
			world.Query(Vector2.zero, 1f, buffer);
			Assert.That(buffer, Does.Contain(food));
			food.Position = Vector2.one * 10000f;
			Assert.That(food.Position.x, Is.EqualTo(Constants.WorldBounds.xMax));
			Assert.That(food.Position.y, Is.EqualTo(Constants.WorldBounds.yMax));
		}

		[Test]
		public void WarmGridQueriesAndMagnetMovementDoNotAllocateManagedMemory()
		{
			var world = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = world.Create(Vector2.zero, 0.2f);
			var buffer = new List<FoodRecord>(32);
			for (int i = 0; i < 100; i++) { food.Position = Vector2.right * (i % 2); world.Query(Vector2.zero, 4f, buffer); }
			long before = GC.GetAllocatedBytesForCurrentThread();
			for (int i = 0; i < 1000; i++) { food.Position = Vector2.right * (i % 2); world.Query(Vector2.zero, 4f, buffer); }
			Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
		}
	}

	public sealed class FoodWireTests
	{
		private readonly FoodPacketWriter _writer = new();
		private void Snapshot(FoodWorld world, FoodReplica replica, ulong epoch, ulong revision, ulong token)
		{
			_writer.Reset(FoodMessage.Begin, epoch, revision); _writer.UInt64(token); _writer.UInt32((uint)world.ActiveCount);
			replica.Receive(_writer.Buffer, _writer.Length);
			uint hash = 2166136261;
			int sent = 0, slot = 0;
			while (slot < world.UsedSlots)
			{
				_writer.Reset(FoodMessage.Chunk, epoch, revision); _writer.UInt64(token); _writer.UInt32((uint)sent);
				int countPosition = _writer.Length; _writer.Byte(0); int start = _writer.Length, count = 0;
				while (slot < world.UsedSlots && count < FoodPacketWriter.RecordsPerPacket)
				{
					FoodRecord food = world.At(slot++); if (!food.Active) continue;
					_writer.Record(food); count++;
				}
				_writer.Buffer[countPosition] = (byte)count;
				hash = FoodPacketWriter.Hash(hash, _writer.Buffer, start, _writer.Length - start);
				replica.Receive(_writer.Buffer, _writer.Length); sent += count;
			}
			_writer.Reset(FoodMessage.End, epoch, revision); _writer.UInt64(token); _writer.UInt32(hash);
			replica.Receive(_writer.Buffer, _writer.Length);
		}
		private void Delta(FoodReplica replica, FoodRecord food, ulong revision, ulong epoch = 1)
		{
			_writer.Reset(FoodMessage.Delta, epoch, revision); _writer.Byte(1); _writer.Record(food);
			replica.Receive(_writer.Buffer, _writer.Length);
		}

		[Test]
		public void TwoThousandRecordSnapshotAndDeltasConvergeForTwoReplicasAndLateJoin()
		{
			var server = new FoodWorld(true, Constants.WorldBounds);
			for (int i = 0; i < 2000; i++) server.Create(new Vector2(i % 40 - 20, i / 40 - 25), 0.2f);
			var first = new FoodReplica(); var second = new FoodReplica();
			Snapshot(server, first, 1, 0, 1);
			Assert.That(first.Ready, Is.True);
			Assert.That(first.World.ActiveCount, Is.EqualTo(2000));
			FoodRecord food = server.At(7);
			food.Position = Vector2.one;
			Delta(first, food, 1);
			Snapshot(server, second, 1, 1, 2);
			server.Renew(food); food.Position = Vector2.right * 2f;
			Delta(first, food, 2); Delta(second, food, 2);
			Assert.That(first.World.Find(food.Id).Position, Is.EqualTo(food.Position));
			Assert.That(second.World.Find(food.Id).Position, Is.EqualTo(food.Position));
			Assert.Throws<InvalidOperationException>(() => first.World.Find(food.Id).Position = Vector2.zero);
		}

		[Test]
		public void GapRequestsRepairAndMigrationEpochRejectsOldPackets()
		{
			var server = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = server.Create(Vector2.zero, 0.2f);
			var replica = new FoodReplica(); Snapshot(server, replica, 1, 0, 1);
			Delta(replica, food, 2);
			Assert.That(replica.NeedsSnapshot, Is.True);
			Snapshot(server, replica, 1, 2, 2);
			Assert.That(replica.NeedsSnapshot, Is.False);
			Snapshot(server, replica, 2, 0, 1);
			food.Position = Vector2.one;
			Delta(replica, food, 3, 1);
			Assert.That(replica.World.Find(food.Id).Position, Is.EqualTo(Vector2.zero));
			Delta(replica, food, 1, 2);
			Assert.That(replica.World.Find(food.Id).Position, Is.EqualTo(Vector2.one));
		}

		[Test]
		public void DuplicateEffectsAreNotReplayedAndMalformedPacketsDoNotPartiallyApply()
		{
			var server = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord food = server.Create(Vector2.zero, 0.2f);
			var replica = new FoodReplica(); Snapshot(server, replica, 1, 0, 1);
			int effects = 0; replica.Eaten += _ => effects++;
			_writer.Reset(FoodMessage.Effects, 1, 1); _writer.Byte(1);
			_writer.Float(0f); _writer.Float(0f); _writer.Float(0.2f); _writer.Byte(0); _writer.Byte(0);
			replica.Receive(_writer.Buffer, _writer.Length); replica.Receive(_writer.Buffer, _writer.Length);
			Assert.That(effects, Is.EqualTo(1));
			_writer.Reset(FoodMessage.Delta, 1, 2); _writer.Byte(2); _writer.Record(food);
			replica.Receive(_writer.Buffer, _writer.Length);
			Assert.That(replica.NeedsSnapshot, Is.True);
			Assert.That(replica.World.ActiveCount, Is.EqualTo(1));
			Assert.That(replica.Revision, Is.EqualTo(1));
		}

		[Test]
		public void IdLayoutRoundTripsAndRejectsHandlesOutsideTheTable()
		{
			ulong id = FoodWorld.MakeId(7, 3);
			Assert.That(FoodWorld.SlotOf(id), Is.EqualTo(7));
			Assert.That(FoodWorld.GenerationOf(id), Is.EqualTo(3u));
			Assert.That(FoodWorld.IsValidId(id), Is.True);
			Assert.That(FoodWorld.IsValidId(FoodWorld.MakeId(FoodWorld.MaximumRecords - 1, 1)), Is.True);
			Assert.That(FoodWorld.IsValidId(FoodWorld.MakeId(FoodWorld.MaximumRecords, 1)), Is.False);
			Assert.That(FoodWorld.IsValidId(FoodWorld.MakeId(0, 0)), Is.False);
			Assert.That(FoodWorld.IsValidId(id & 0x7fffffffffffffffUL), Is.False);
		}
	}
}
