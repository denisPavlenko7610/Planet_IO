using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace PlanetIO.Tests
{
    public sealed class MatchMigrationTests
    {
        private static MatchSnapshot Valid()
        {
            return new MatchSnapshot
            {
                Ready = true, MatchId = "match", Clock = 25,
                Players = new[]
                {
                    new PlanetMigrationState
                    {
                        ObjectId = 1, Identity = "member", Mass = 1f, Tier = 3,
                        Durability = new byte[] { 1, 0, MoonRules.MaxDurability, MoonRules.MaxDurability, 0, 0, 0, 0 },
                        Regrow = new float[] { 0, 4, 0, 0, 0, 0, 0, 0 }, MoonSlots = 4,
                        Perks = 5, ContinueUsed = true, Offer = new byte[] { 1, 2, 3 }, OfferCount = 3,
                        OfferRemaining = 2f
                    }
                },
                Fields = new FoodFieldMigrationState
                    { Centers = Array.Empty<Vector2>(), Radii = Array.Empty<float>(), Counts = Array.Empty<int>() },
                World = new WorldMigrationState
                {
                    PortalIds = Array.Empty<ulong>(), PortalRemaining = Array.Empty<double>(),
                    BlackHoleVictims = Array.Empty<ulong>()
                }
            };
        }

        [Test] public void CodecPreservesProgressAndMoonTimers()
        {
            var restored = MatchSnapshotCodec.Decode(MatchSnapshotCodec.Encode(Valid()));
            Assert.That(restored.Players[0].Mass, Is.EqualTo(1f));
            Assert.That(restored.Players[0].Durability[0], Is.EqualTo(1));
            Assert.That(restored.Players[0].Regrow[1], Is.EqualTo(4f));
            Assert.That(restored.Players[0].Perks, Is.EqualTo(5));
            Assert.That(restored.Players[0].ContinueUsed, Is.True);
            Assert.That(restored.Players[0].OfferRemaining, Is.EqualTo(2f));
        }

		[Test]
		public void CodecPreservesTwoThousandFoodAndTemporaryDropsWithinTransportBudget()
		{
			MatchSnapshot snapshot = Valid();
			var random = new System.Random(20261117);
			snapshot.Food = new FoodMigrationState[2060];
			for (int index = 0; index < snapshot.Food.Length; index++)
			{
				bool dropped = index >= 2000;
				snapshot.Food[index] = new FoodMigrationState
				{
					ObjectId = FoodWorld.MakeId(index, 1),
					Position = new Vector2((float)random.NextDouble() * 440f - 220f, (float)random.NextDouble() * 296f - 136f),
					Scale = 0.1f + (float)random.NextDouble(),
					Nutrition = dropped ? (float)random.NextDouble() : 0f,
					Value = index % 25 == 0 ? 8f : 1f,
					Remaining = dropped ? (float)random.NextDouble() * 20f : 0f,
					Immunity = dropped ? (float)random.NextDouble() : 0f,
					Cell = dropped ? -1 : index % 1000,
					Dropped = dropped,
					DropperId = ulong.MaxValue
				};
			}

			byte[] encoded = MatchSnapshotCodec.Encode(snapshot);
			Assert.That(encoded.Length, Is.LessThanOrEqualTo(MatchSnapshotCodec.MaximumCompressedBytes));
			MatchSnapshot restored = MatchSnapshotCodec.Decode(encoded);
			Assert.That(restored.Food.Length, Is.EqualTo(2060));
			for (int index = 0; index < snapshot.Food.Length; index++)
			{
				Assert.That(restored.Food[index].Position, Is.EqualTo(snapshot.Food[index].Position));
				Assert.That(restored.Food[index].Scale, Is.EqualTo(snapshot.Food[index].Scale));
				Assert.That(restored.Food[index].Nutrition, Is.EqualTo(snapshot.Food[index].Nutrition));
				Assert.That(restored.Food[index].Remaining, Is.EqualTo(snapshot.Food[index].Remaining));
			}
		}

		[Test]
		public void BinaryFoodMigrationPreservesTombstonesAndRejectsAmbiguousSlots()
		{
			var world = new FoodWorld(true, Constants.WorldBounds);
			FoodRecord active = world.Create(Vector2.one, 0.3f, 2);
			FoodRecord removed = world.Create(Vector2.zero, 0.2f);
			world.Remove(removed);
			MatchSnapshot snapshot = Valid();
			snapshot.Food = new[] { world.Capture(active), world.Capture(removed) };
			MatchSnapshot restored = MatchSnapshotCodec.Decode(MatchSnapshotCodec.Encode(snapshot));
			Assert.That(restored.Food[1].Inactive, Is.True);
			Assert.That(restored.Food[0].Flavor, Is.EqualTo(2));
			restored.Food[1].ObjectId = FoodWorld.MakeId(0, 2);
			Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Encode(restored));
			restored.Food[1].ObjectId = 123;
			Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Encode(restored));
			restored.Food = Array.Empty<FoodMigrationState>();
			restored.Version = 1;
			Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Encode(restored));
		}

        [Test] public void InvalidVersionDuplicateIdentityAndNanAreRejected()
        {
            var s = Valid();
            s.Version = MatchSnapshot.CurrentVersion + 1;
            Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Encode(s));
            s = Valid();
            s.Players[0].Mass = float.NaN;
            Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Encode(s));
            s = Valid();
            s.Players = new[] { s.Players[0], s.Players[0] };
            Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Encode(s));
            Assert.Throws<InvalidDataException>(() => MatchSnapshotCodec.Decode(Array.Empty<byte>()));
        }

        [Test] public void SnapshotCannotBeAppliedToAnotherMatchOrReplayOlderRevision()
        {
            var store = new MatchMigrationStore();
            store.StartMatch("other");
            Assert.Throws<InvalidDataException>(() => store.Accept(MatchSnapshotCodec.Encode(Valid())));
            store.StartMatch("match");
            var s = Valid();
            s.Revision = 4;
            store.Accept(MatchSnapshotCodec.Encode(s));
            s.Revision = 3;
            Assert.Throws<InvalidDataException>(() => store.Accept(MatchSnapshotCodec.Encode(s)));
        }

        [Test] public void PlayerRebindConsumesStateOnceAndStableIdsSurviveNetworkIdChanges()
        {
            var store = new MatchMigrationStore();
            store.StartMatch("match");
            store.Accept(MatchSnapshotCodec.Encode(Valid()));
            Assert.That(store.TakePlayer("imposter"), Is.Null);
            Assert.That(store.TakePlayer("member").Perks, Is.EqualTo(5));
            Assert.That(store.TakePlayer("member"), Is.Null);
            store.BindObject(1, 99);
            Assert.That(store.StableId(99), Is.EqualTo(1));
            Assert.That(
                store.StableId(1),
                Is.Not.EqualTo(1),
                "An NGO ID recycled by a new host is not an existing entity ID."
            );
        }

        [Test] public void LocalRunStateSurvivesSceneDisposalAndIsClearedForANewMatch()
        {
            var store = new MatchMigrationStore();
            store.StartMatch("match");
            store.RegisterLocal("run", () => "run-123", value => { });
            store.BeginRecovery();
            store.UnregisterLocal("run");
            string resumed = null;
            store.RegisterLocal("run", () => "new", value => resumed = value);
            Assert.That(resumed, Is.EqualTo("run-123"));
            store.StartMatch("new-match");
            resumed = null;
            store.RegisterLocal("run", () => "fresh", value => resumed = value);
            Assert.That(resumed, Is.Null);
        }
    }
}
