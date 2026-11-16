using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace PlanetIO
{
    [Serializable] public sealed class MatchSnapshot
    {
        public int Version = 1;
        public bool Ready;
        public string MatchId;
        public long Revision;
        public double Clock;
        public BotDifficulty Difficulty;
        public PlanetMigrationState[] Players = Array.Empty<PlanetMigrationState>();
        public PlanetMigrationState[] Bots = Array.Empty<PlanetMigrationState>();
        public FoodMigrationState[] Food = Array.Empty<FoodMigrationState>();
        public CometMigrationState[] Comets = Array.Empty<CometMigrationState>();
        public FoodFieldMigrationState Fields;
        public WorldMigrationState World;
    }

    [Serializable] public sealed class PlanetMigrationState
    {
        public ulong ObjectId;
        public string Identity, Name;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector2 Velocity, Direction, DesiredDirection;
        public float Mass, MoonPhase, MoonImmunity, ClashCooldown;
        public int Tier, MoonSlots;
        public byte[] Durability;
        public float[] Regrow;
        public bool Defeated, ContinueUsed, Boosting;
        public float DefeatMass, Protection, BoostTimer;
        public ushort Perks;
        public int HighestTier, OfferCount, PendingOffers;
        public byte[] Offer;
        public float OfferRemaining, PendingRestore, StealthRemaining, StealthCooldown;
        public Vector2[] TrailPositions;
        public float[] TrailRemaining;
        public int TrailNext;
        public float TrailSpawnTimer, TrailStrikeTimer;
        public Color32 Color;
        public byte Skin;
        public ulong[] LootIds;
        public ulong AppearanceSeed;
        public int Personality, MovementState;
        public float StateRemaining, ThinkRemaining, DashRemaining, DashDelay;
        public bool DashEnabled;
        public float BoostRemaining, BoostCooldown;
        public ulong TargetId;
    }

    [Serializable] public sealed class FoodMigrationState
    {
        public ulong ObjectId, DropperId;
        public Vector2 Position;
        public float Scale, Nutrition, Value, Remaining, Immunity;
        public int Cell;
        public bool Dropped, CometTrail;
    }

    [Serializable] public sealed class CometMigrationState
    {
        public ulong ObjectId;
        public Vector2 Position, Direction;
        public float Mass, Speed, FoodTimer, RetryTimer;
        public bool Waiting, Meteor;
    }

    [Serializable] public sealed class FoodFieldMigrationState
    {
        public Vector2[] Centers;
        public float[] Radii;
        public int[] Counts;
        public float RelocateTimer;
    }

    [Serializable] public sealed class WorldMigrationState
    {
        public int Seed, Modifier, Phase, ActiveIndex;
        public float AbsorbedMass;
        public ulong[] BlackHoleVictims, PortalIds;
        public double[] PortalRemaining;
    }

    public interface IMigrationLootDrop
    {
        ulong[] CaptureFoodIds();
    }

    public interface IMigrationLootFactory
    {
        ILootDrop RestoreLoot(ulong[] oldIds, IReadOnlyDictionary<ulong, ulong> objectIds);
    }

    public static class MatchSnapshotCodec
    {
        public const int MaximumCompressedBytes = 120 * 1024;
        public const int MaximumJsonBytes = 4 * 1024 * 1024;

        public static byte[] Encode(MatchSnapshot snapshot)
        {
            Validate(snapshot);
            byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(snapshot));
            if (json.Length > MaximumJsonBytes)
            {
                throw new InvalidDataException("Migration snapshot is too large.");
            }

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, true))
                gzip.Write(json, 0, json.Length);
            byte[] bytes = output.ToArray();
            if (bytes.Length > MaximumCompressedBytes)
            {
                throw new InvalidDataException("Compressed migration snapshot is too large.");
            }

            return bytes;
        }

        public static MatchSnapshot Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > MaximumCompressedBytes)
            {
                throw new InvalidDataException("Invalid migration payload size.");
            }

            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            byte[] buffer = new byte[4096];
            int count;
            while ((count = gzip.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (output.Length + count > MaximumJsonBytes)
                {
                    throw new InvalidDataException("Migration payload exceeds its expanded limit.");
                }

                output.Write(buffer, 0, count);
            }
            var snapshot = JsonUtility.FromJson<MatchSnapshot>(Encoding.UTF8.GetString(output.ToArray()));
            Validate(snapshot);
            return snapshot;
        }

        public static void Validate(MatchSnapshot value)
        {
            if (value == null ||
                value.Version != 1 ||
                !value.Ready ||
                string.IsNullOrEmpty(value.MatchId) ||
                double.IsNaN(value.Clock) ||
                double.IsInfinity(value.Clock) ||
                value.Clock < 0 ||
                value.Players == null ||
                value.Players.Length > 64 ||
                value.Bots == null ||
                value.Bots.Length > 128 ||
                value.Food == null ||
                value.Food.Length > 8192 ||
                value.Comets == null ||
                value.Comets.Length > 128 ||
                value.Fields == null ||
                value.World == null)
            {
                throw new InvalidDataException("Incomplete or incompatible migration snapshot.");
            }

            var ids = new HashSet<ulong>();
            var identities = new HashSet<string>(StringComparer.Ordinal);
            foreach (var player in value.Players)
            {
                ValidatePlanet(player, ids);
                if (string.IsNullOrEmpty(player.Identity) ||
                    player.Identity.Length > 128 ||
                    !identities.Add(player.Identity))
                {
                    throw new InvalidDataException("Duplicate or missing player identity.");
                }
            }
            foreach (var bot in value.Bots)
            {
                ValidatePlanet(bot, ids);
            }
            foreach (var food in value.Food)
            {
                if (food == null ||
                    !ids.Add(food.ObjectId) ||
                    !Finite(food.Position) ||
                    !Finite(food.Scale) ||
                    food.Scale <= 0 ||
                    !Finite(food.Nutrition) ||
                    food.Nutrition < 0 ||
                    !Finite(food.Value) ||
                    food.Value < 1 ||
                    !Finite(food.Remaining) ||
                    food.Remaining < 0 ||
                    !Finite(food.Immunity))
                {
                    throw new InvalidDataException("Invalid food state.");
                }
            }

            foreach (var comet in value.Comets)
            {
                if (comet == null ||
                    !ids.Add(comet.ObjectId) ||
                    !Finite(comet.Position) ||
                    !Finite(comet.Direction) ||
                    !Finite(comet.Mass) ||
                    comet.Mass <= 0 ||
                    !Finite(comet.Speed) ||
                    comet.Speed < 0)
                {
                    throw new InvalidDataException("Invalid comet state.");
                }
            }

            if (value.Fields.Centers == null ||
                value.Fields.Radii == null ||
                value.Fields.Centers.Length != value.Fields.Radii.Length ||
                value.Fields.Centers.Length > 128 ||
                value.Fields.Counts == null ||
                value.Fields.Counts.Length > 16384 ||
                value.World.PortalIds == null ||
                value.World.PortalRemaining == null ||
                value.World.PortalIds.Length != value.World.PortalRemaining.Length ||
                value.World.BlackHoleVictims == null)
            {
                throw new InvalidDataException("Invalid world state.");
            }
        }

        private static void ValidatePlanet(PlanetMigrationState p, HashSet<ulong> ids)
        {
            if (p == null ||
                !ids.Add(p.ObjectId) ||
                !Finite(p.Position) ||
                !Finite(p.Velocity) ||
                !Finite(p.Mass) ||
                p.Mass < 0.01f ||
                p.Mass > PlanetSizeRules.MaximumCapacity ||
                p.Tier < 1 ||
                p.Tier > 4 ||
                p.MoonSlots < 0 ||
                p.MoonSlots > MoonLoadout.MaxSlots ||
                p.Durability == null ||
                p.Regrow == null ||
                p.Durability.Length != MoonLoadout.MaxSlots ||
                p.Regrow.Length != MoonLoadout.MaxSlots)
            {
                throw new InvalidDataException("Invalid planet state.");
            }

            for (int i = 0; i < MoonLoadout.MaxSlots; i++)
            {
                if (p.Durability[i] > MoonRules.MaxDurability || !Finite(p.Regrow[i]) || p.Regrow[i] < 0)
                {
                    throw new InvalidDataException("Invalid moon state.");
                }
            }
        }

        private static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
        private static bool Finite(Vector3 v) => Finite(v.x) && Finite(v.y) && Finite(v.z);
        private static bool Finite(Vector2 v) => Finite(v.x) && Finite(v.y);
    }
}
