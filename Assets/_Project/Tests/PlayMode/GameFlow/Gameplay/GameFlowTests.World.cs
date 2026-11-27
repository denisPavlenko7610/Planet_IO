using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlanetIO.Application;
using UnityEngine;
using UnityEngine.TestTools;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        private static int TodaySeed() =>
            WorldSeedRules.FromDay((int)(DateTime.UtcNow - DateTime.UnixEpoch).TotalDays);

        private static WorldDirector ResolveDirector() => (WorldDirector)ResolveGame<IWorldEvents>();

        private static void InvokeDirector(WorldDirector director, string method, params object[] arguments)
        {
            MethodInfo info = typeof(WorldDirector).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(director, arguments);
        }

        private static void SetDirectorField(WorldDirector director, string name, object value)
        {
            FieldInfo field = typeof(WorldDirector).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(director, value);
        }

        private static void ForceEvent(WorldDirector director, CosmicEvent worldEvent)
        {
            SetDirectorField(director, "_event", worldEvent);
            Type phase = typeof(WorldDirector).GetNestedType("Phase", BindingFlags.NonPublic);
            SetDirectorField(director, "_phase", Enum.ToObject(phase, 2));
        }

        private static CosmicEvent FindEvent(CosmicEventType type)
        {
            int seed = TodaySeed();
            for (int index = 0; index < 12; index++)
            {
                CosmicEvent candidate = CosmicEventRules.Get(seed, index);
                if (candidate.Type == type)
                {
                    return candidate;
                }
            }

            Assert.Fail($"No {type} in the first events.");
            return default;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_BuildsMapFeaturesAndStarAnchors()
        {
            yield return StartMoonGame(dailyWorld: true);

            MapFeatureField.Anchor[] anchors = MapFeatureField.Anchors;
            for (int slot = 0; slot < MapFeatureRules.StarCount; slot++)
            {
                Assert.That(anchors[slot].Strength, Is.GreaterThan(0f), $"star {slot}");
                Assert.That(anchors[slot].ZoneRadius, Is.EqualTo(MapFeatureRules.StarZoneRadius));
            }

            Assert.That(anchors[MapFeatureField.BlackHoleSlot].Strength, Is.Zero);
            Assert.That(GameObject.Find("WorldFeatures"), Is.Not.Null);
            Assert.That(
                MapFeatureField.GravityScale,
                Is.EqualTo(DailyModifierRules.GravityScale(ResolveGame<IWorldEvents>().Modifier)));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_NebulaHidesPlanetsFromRivals()
        {
            yield return StartMoonGame(dailyWorld: true);
            Player player = PrepareMoonPlayer();
            var discs = new MapDisc[MapFeatureRules.NebulaCount];
            MapFeatureRules.Nebulae(TodaySeed(), discs);

            player.transform.position = discs[0].Center;
            Physics2D.SyncTransforms();
            Assert.That(player.IsHiddenFromRivals, Is.True);

			Vector2 outside = new(WorldBounds.Area.xMax - 2f, WorldBounds.Area.yMax - 2f);
            bool outsideAll = !MapFeatureRules.InsideAny(discs, outside);
            if (outsideAll)
            {
                player.transform.position = outside;
                Assert.That(player.IsHiddenFromRivals, Is.False);
            }
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_StarPullsAnyBodyTowardIt()
        {
            yield return StartMoonGame(dailyWorld: true);
            Player player = PrepareMoonPlayer();
            MapFeatureField.Anchor star = MapFeatureField.Anchors[0];
            float distance = star.Radius + (star.ZoneRadius - star.Radius) * 0.5f;
            Vector2 start = star.Position + Vector2.right * distance;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            player.transform.position = start;
            body.position = start;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            PlanetGravity gravity = player.GetComponent<PlanetGravity>();
            MethodInfo step = typeof(PlanetGravity).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

            step.Invoke(gravity, null);

            Vector2 moved = body.position - start;
            float expected = GravityRules.PullSpeed(distance, star.Radius, star.ZoneRadius) *
                             star.Strength * MapFeatureField.GravityScale * Time.fixedDeltaTime;
            Assert.That(moved.x, Is.LessThan(0f), "Pulled toward the star.");
            Assert.That(moved.magnitude, Is.EqualTo(expected).Within(expected * 0.25f + 1e-4f));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_WormholeTeleportsOncePerCooldown()
        {
            yield return StartMoonGame(dailyWorld: true);
            Player player = PrepareMoonPlayer();
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            WorldDirector director = ResolveDirector();
            MapFeatureRules.Wormholes(TodaySeed(), out Vector2 portalA, out Vector2 portalB);
            player.transform.position = portalA;
            player.GetComponent<Rigidbody2D>().position = portalA;
            Physics2D.SyncTransforms();
            double now = OrbitClock.Now;

            float reach = MapFeatureRules.WormholeExitOffset + player.BodyRadius + 0.5f;
            InvokeDirector(director, "UpdateWormholes", now);
            yield return WaitUntil(() => Vector2.Distance(player.transform.position, portalB) <= reach, 3f);

            Assert.That(Vector2.Distance(player.transform.position, portalB), Is.LessThanOrEqualTo(reach));

            player.transform.position = portalB;
            player.GetComponent<Rigidbody2D>().position = portalB;
            Physics2D.SyncTransforms();
            InvokeDirector(director, "UpdateWormholes", now + 1.0);
            yield return new WaitForSeconds(0.3f);

            Assert.That(Vector2.Distance(player.transform.position, portalB), Is.LessThan(0.5f), "Cooldown blocks the return trip.");

            InvokeDirector(director, "UpdateWormholes", now + MapFeatureRules.WormholeCooldownSeconds + 1.0);
            yield return WaitUntil(() => Vector2.Distance(player.transform.position, portalA) <= reach, 3f);

            Assert.That(Vector2.Distance(player.transform.position, portalA), Is.LessThanOrEqualTo(reach));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_BlackHoleTakesAQuarterOncePerEventAndSpitsLoot()
        {
            yield return StartMoonGame(dailyWorld: true);
            Player player = PrepareMoonPlayer();
            player.Capacity = 1f;
            WorldDirector director = ResolveDirector();
            CosmicEvent blackHole = FindEvent(CosmicEventType.BlackHole);
            ForceEvent(director, blackHole);
            Vector2 center = blackHole.PositionAt(blackHole.StartTime);
            player.transform.position = center;
            player.GetComponent<Rigidbody2D>().position = center;
            Physics2D.SyncTransforms();
            SetMoonField(player, "_invincibilityTimeRemaining", 0f);

            InvokeDirector(director, "DamageInsideBlackHole", blackHole.StartTime);
            float afterFirst = player.Capacity;
            InvokeDirector(director, "DamageInsideBlackHole", blackHole.StartTime + 1.0);

            Assert.That(afterFirst, Is.EqualTo(0.75f).Within(0.01f));
            Assert.That(player.Capacity, Is.EqualTo(afterFirst).Within(1e-5f), "Only one bite per event.");

            float dropped = SumDroppedNutrition();
            InvokeDirector(director, "ServerFinish");

            Assert.That(SumDroppedNutrition(), Is.GreaterThan(dropped), "The hole spits out what it absorbed.");
            yield return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_MeteorShowerLaunchesCometsAcrossTheBandAndRestoresThem()
        {
            yield return StartMoonGame(dailyWorld: true);
            WorldDirector director = ResolveDirector();
            CosmicEvent meteors = FindEvent(CosmicEventType.MeteorShower);
            ForceEvent(director, meteors);

            InvokeDirector(director, "LaunchMeteors");

            Comet[] comets = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude);
            int launched = 0;
            foreach (Comet comet in comets)
            {
                CometMovement movement = comet.GetComponent<CometMovement>();
                FieldInfo speedField = typeof(CometMovement).GetField("_normalSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
                float speed = (float)speedField.GetValue(movement);
                if (speed >= CosmicEventRules.MeteorSpeed - 1.01f)
                {
                    launched++;
                    FieldInfo directionField = typeof(CometMovement).GetField("_direction", BindingFlags.Instance | BindingFlags.NonPublic);
                    Vector2 direction = (Vector2)directionField.GetValue(movement);
                    Assert.That(Vector2.Dot(direction, meteors.Direction), Is.GreaterThan(0.999f));
                }
            }

            Assert.That(launched, Is.GreaterThanOrEqualTo(Mathf.Min(12, comets.Length)));

            float dropped = SumDroppedNutrition();
            InvokeDirector(director, "ServerFinish");

            foreach (Comet comet in comets)
            {
                FieldInfo speedField = typeof(CometMovement).GetField("_normalSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
                float speed = (float)speedField.GetValue(comet.GetComponent<CometMovement>());
                Assert.That(speed, Is.LessThan(CosmicEventRules.MeteorSpeed - 1.01f), "Comets slow down after the shower.");
            }

            Assert.That(SumDroppedNutrition(), Is.GreaterThan(dropped), "A shower leaves loot along its path.");
            yield return null;
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator World_EventPhasesFireWarningStartAndEndInOrder()
        {
            yield return StartMoonGame(dailyWorld: true);
            WorldDirector director = ResolveDirector();
            CosmicEvent supernova = FindEvent(CosmicEventType.Supernova);
            var order = new List<string>();
            director.Warned += type => order.Add("warn:" + type);
            director.Started += type => order.Add("start:" + type);
            director.Ended += type => order.Add("end:" + type);
            int seed = TodaySeed();
            Assert.That(CosmicEventRules.TryFindActive(seed, supernova.StartTime + 1.0, out _), Is.True);

            InvokeDirector(director, "UpdatePhase", supernova.WarningTime + 0.1);
            Assert.That(director.HasMarker, Is.True);
            Assert.That(director.MarkerType, Is.EqualTo(CosmicEventType.Supernova));
            InvokeDirector(director, "UpdatePhase", supernova.StartTime + 0.1);
            InvokeDirector(director, "UpdatePhase", supernova.EndTime + 1.0);

            Assert.That(order, Is.EqualTo(new[] { "warn:Supernova", "start:Supernova", "end:Supernova" }));
            Assert.That(director.HasMarker, Is.False);
            yield return null;
        }
    }
}
