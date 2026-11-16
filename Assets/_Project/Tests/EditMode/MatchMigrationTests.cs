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
                        Durability = new byte[] { 1, 0, 3, 3, 0, 0, 0, 0 },
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

        [Test] public void InvalidVersionDuplicateIdentityAndNanAreRejected()
        {
            var s = Valid();
            s.Version = 2;
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
