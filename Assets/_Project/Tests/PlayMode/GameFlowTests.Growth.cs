using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlanetIO.UI.Hud;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, Explicit("Manual balance measurement: ten simulated minutes of uninterrupted feeding."), Timeout(240000)]
        public IEnumerator GrowthPace_ContinuousFeedingRecordsMilestones()
        {
            yield return StartMoonGame();
            Player player = FindLocalPlayer();
            player.Capacity = 0.081f;
            SetMoonField(player, "_invincibilityTimeRemaining", 10000f);
            foreach (Enemy enemy in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
            {
                enemy.GetComponent<Collider2D>().enabled = false;
                enemy.GetComponent<EnemyMovement>().enabled = false;
                enemy.enabled = false;
            }
            foreach (Comet comet in Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude))
                comet.GetComponent<Collider2D>().enabled = false;
            player.GetComponent<PlanetGravity>().enabled = false;
            var scope = Object.FindFirstObjectByType<PlanetIO.Bootstrap.GameLifetimeScope>();
            ((System.IDisposable)scope.Container.Resolve<IWorldEvents>()).Dispose();
            MapFeatureField.Reset();
            Food[] foods = Object.FindObjectsByType<Food>(FindObjectsInactive.Exclude);
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            Food target = foods.OrderBy(food => (food.transform.position - player.transform.position).sqrMagnitude).First();
            player.transform.position = target.transform.position + Vector3.left * 2f;
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            float start = Time.time;
            float oldScale = Time.timeScale;
            float oldMaximum = Time.maximumDeltaTime;
            float[] thresholds = { 0.35f, 0.8f, 1.4f, 2f };
            float[] reached = { -1f, -1f, -1f, -1f };
            int[] sampleTimes = { 60, 180, 240, 360, 480, 600 };
            int sample = 0;
            var report = new System.Text.StringBuilder("Automated nearest-food pilot; no boosts, opponents or events; no perks; 8x simulation.\n");
            Time.timeScale = 8f;
            Time.maximumDeltaTime = 0.1f;
            try
            {
                while (Time.time - start < 600f && !player.IsDefeated)
                {
                    float elapsed = Time.time - start;
                    for (int i = 0; i < thresholds.Length; i++)
                        if (reached[i] < 0f && player.Capacity >= thresholds[i]) reached[i] = elapsed;
                    if (sample < sampleTimes.Length && elapsed >= sampleTimes[sample])
                    {
                        report.AppendLine($"t={sampleTimes[sample]}s mass={player.Capacity:F4}");
                        sample++;
                        System.IO.File.WriteAllText("Temp/GrowthPace.txt", report.ToString());
                    }
                    target = foods.Where(food => !food.IsDropped && food.gameObject.activeInHierarchy)
                        .OrderBy(food => (food.transform.position - player.transform.position).sqrMagnitude).First();
                    movement.SetDirection(target.transform.position - player.transform.position);
                    // Suppress offers so the probe keeps identical multipliers for the whole run.
                    SetMoonField(player, "_pendingOffers", 0);
                    SetMoonField(player, "_offerCount", 0);
                    yield return new WaitForFixedUpdate();
                }
                report.AppendLine($"End: t={Time.time - start:F1}s mass={player.Capacity:F4}; defeated={player.IsDefeated}");
                for (int i = 0; i < thresholds.Length; i++) report.AppendLine($"mass={thresholds[i]:F2} reached={reached[i]:F1}s (-1 = not reached)");
                System.IO.File.WriteAllText("Temp/GrowthPace.txt", report.ToString());
                Debug.Log(report.ToString());
                Assert.That(player.IsDefeated, Is.False);
                Assert.That(player.Capacity, Is.GreaterThan(0.081f));
            }
            finally
            {
                Time.timeScale = oldScale;
                Time.maximumDeltaTime = oldMaximum;
            }
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Growth_MassNeverChangesActualBaseOrBoostVelocity()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            MethodInfo step = typeof(PlayerMovement).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (float mass in new[] { 0.1f, 0.35f, 0.8f, 2f })
            {
                player.Capacity = mass;
                player.SetBoostRpc(false);
                step.Invoke(movement, null);
                Assert.That(body.linearVelocity.magnitude, Is.EqualTo(4.8f).Within(0.001f));
                typeof(PlayerMovement).GetField("_isBoosting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, true);
                player.SetBoostRpc(true);
                step.Invoke(movement, null);
                Assert.That(body.linearVelocity.magnitude, Is.EqualTo(9.6f).Within(0.001f));
            }
            GrantPerk(player, PerkId.IonTrail);
            step.Invoke(movement, null);
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(11.04f).Within(0.001f));
            player.SetBoostRpc(false);
            step.Invoke(movement, null);
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(4.8f).Within(0.001f));
            GrantPerk(player, PerkId.SolarSail);
            step.Invoke(movement, null);
            Assert.That(body.linearVelocity.magnitude, Is.EqualTo(5.376f).Within(0.001f));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Growth_CameraShowsLargerBodyAndPerksLeaveCenterClear()
        {
            yield return StartMoonGame();
            Player player = PrepareMoonPlayer();
            SetMoonField(player, "_invincibilityTimeRemaining", 1000f);
            player.GetComponent<PlayerMovement>().enabled = false;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            player.GetComponent<Collider2D>().enabled = false;
            player.GetComponent<PlanetGravity>().enabled = false;
            Camera camera = Camera.main;
            float baseline = 0f;
            foreach (float mass in new[] { 0.081f, 0.35f, 0.8f, 2f })
            {
                player.Capacity = mass;
                yield return new WaitForSeconds(3.5f);
                float diameter = player.GetComponent<SpriteRenderer>().bounds.size.x / (2f * camera.orthographicSize);
                if (baseline == 0f) baseline = diameter;
                Debug.Log($"Growth view: mass={mass:F2}, zoom={camera.orthographicSize:F3}, relative diameter={diameter / baseline:F3}");
                if (mass == 2f) Assert.That(diameter / baseline, Is.GreaterThan(2.7f));
            }
            SessionHudView hud = Object.FindFirstObjectByType<SessionHudView>();
            hud.SetPerkIcons(new[] { 0, 4, 2 });
            hud.ShowPerkOffer("Choose a perk", new[] { "Ion trail", "Solar sail", "Heavy moons" },
                new[] { "Boost +15%; trail 2 s; cost +25%", "Speed +12%; food -10%", "+1 moon; damage +50%; speed -5%" });
            yield return null;
            yield return null;
            VisualElement root = hud.GetComponent<UIDocument>().rootVisualElement;
            VisualElement box = root.Q("perk-box");
            Assert.That(box.worldBound.yMax, Is.LessThan(root.worldBound.height * 0.46f));
            Assert.That(root.Q("perk-icon-0").resolvedStyle.backgroundImage.vectorImage, Is.Not.Null);
            Assert.That(root.Q("perk-card-0").worldBound.height, Is.GreaterThan(90f));
            ScreenCapture.CaptureScreenshot("Temp/PerkGrowth.png");
            yield return null;
            yield return null;
            hud.HidePerkOffer();
            Assert.That(root.ClassListContains("perk-offer-visible"), Is.False);
        }
    }
}
