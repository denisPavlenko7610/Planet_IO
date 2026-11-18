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
		[UnityTest, Timeout(60000)]
		public IEnumerator Feel_MenuAndSettingsHaveComfortableTouchTargets()
		{
			yield return OpenMenu();
			System.IO.Directory.CreateDirectory("Temp/FeelAudit");
			ScreenCapture.CaptureScreenshot("Temp/FeelAudit/Menu.png");
			yield return null;
			yield return null;
			Click(FindButton("settings-button"));
			yield return null;
			yield return null;
			var root = FindButton("settings-close").panel.visualTree;
			var toggle = root.Q("fixed-joystick-toggle");
			Assert.That(toggle.worldBound.height, Is.GreaterThanOrEqualTo(87.25f));
			Assert.That(root.Q("previous-language").worldBound.height, Is.GreaterThanOrEqualTo(95.25f));
			ScreenCapture.CaptureScreenshot("Temp/FeelAudit/Settings.png");
			yield return null;
			yield return null;
			var menu = root.Q("menu-root");
			foreach (Vector2 size in new[] { new Vector2(1280, 2276), new Vector2(1920, 1080), new Vector2(2400, 1080) })
			{
				menu.style.width = size.x;
				menu.style.height = size.y;
				yield return null;
				yield return null;
				foreach (string name in new[] { "settings-panel", "settings-close", "settings-button", "play-with-bots", "online-button", "profile-button" })
					AssertUiInside(menu.Q(name), menu);
				Assert.That(menu.Q("profile-button").worldBound.yMin,
					Is.GreaterThanOrEqualTo(menu.Q("header").worldBound.yMax));
			}
			menu.style.width = StyleKeyword.Null;
			menu.style.height = StyleKeyword.Null;
			Click(FindButton("settings-close"));
		}

		private static void AssertUiInside(VisualElement element, VisualElement screen)
		{
			Rect bounds = element.worldBound;
			Rect viewport = screen.worldBound;
			Assert.That(bounds.width, Is.GreaterThan(0), element.name);
			Assert.That(bounds.height, Is.GreaterThan(0), element.name);
			Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(viewport.xMin - 1.5f), element.name);
			Assert.That(bounds.xMax, Is.LessThanOrEqualTo(viewport.xMax + 1.5f), element.name);
			Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(viewport.yMin - 1.5f), element.name);
			Assert.That(bounds.yMax, Is.LessThanOrEqualTo(viewport.yMax + 1.5f), element.name);
		}

		[UnityTest, Timeout(60000)]
		public IEnumerator Feel_GrowthAndMinimapStayReadable()
		{
			yield return StartMoonGame();
			Player player = PrepareMoonPlayer();
			SetMoonField(player, "_invincibilityTimeRemaining", 1000f);
			player.GetComponent<PlayerMovement>().enabled = false;
			player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
			player.GetComponent<PlanetGravity>().enabled = false;
			player.Capacity = 0.081f;
			float before = player.transform.localScale.x;
			player.Capacity = 0.8f;
			Assert.That(player.Capacity, Is.EqualTo(0.8f));
			Assert.That(player.transform.localScale.x, Is.EqualTo(before));
			yield return new WaitForSeconds(0.1f);
			Assert.That(player.transform.localScale.x, Is.InRange(before + 0.001f, PlanetSizeRules.Scale(0.8f) - 0.001f));
			yield return new WaitForSeconds(0.8f);
			Assert.That(player.transform.localScale.x, Is.EqualTo(PlanetSizeRules.Scale(player.Capacity)).Within(0.003f));
			player.Capacity = 0.081f;
			Assert.That(player.transform.localScale.x, Is.EqualTo(PlanetSizeRules.Scale(0.081f)).Within(0.0001f));
			var hud = Object.FindFirstObjectByType<SessionHudView>();
			SetMoonField(player, "_pendingOffers", 0);
			SetMoonField(player, "_offerCount", 0);
			hud.HidePerkOffer();
			var root = hud.GetComponent<UIDocument>().rootVisualElement;
			var map = root.Q("minimap");
			float worldAspect = Constants.WorldBounds.width / Constants.WorldBounds.height;
			float logicalPixel = root.worldBound.width / Screen.width;
			// Snapping both rectangle edges can change each dimension by one physical pixel.
			float minimumAspect = (map.layout.width - logicalPixel) / (map.layout.height + logicalPixel);
			float maximumAspect = (map.layout.width + logicalPixel) / (map.layout.height - logicalPixel);
			Assert.That(worldAspect, Is.InRange(minimumAspect, maximumAspect));
			Assert.That(root.Q("settings-button").worldBound.height, Is.GreaterThanOrEqualTo(100f));
			Assert.That(root.Q("stick").layout.width, Is.EqualTo(264f).Within(1.5f));
			Assert.That(root.Q("stick-knob").layout.width, Is.EqualTo(108f).Within(1.5f));
			var screen = root.Q("hud-root");
			foreach (Vector2 size in new[] { new Vector2(1280, 2276), new Vector2(1920, 1080), new Vector2(2400, 1080) })
			{
				screen.style.width = size.x;
				screen.style.height = size.y;
				foreach (bool swapped in new[] { false, true })
				{
					screen.EnableInClassList("controls-swapped", swapped);
					yield return null;
					yield return null;
					foreach (string name in new[] { "stick", "boost-button", "settings-button", "leave-button", "minimap" })
						AssertUiInside(screen.Q(name), screen);
					Assert.That(screen.Q("stick").worldBound.Overlaps(screen.Q("boost-button").worldBound), Is.False);
				}
			}
			screen.style.width = StyleKeyword.Null;
			screen.style.height = StyleKeyword.Null;
			screen.EnableInClassList("controls-swapped", false);
			yield return null;
			yield return null;
			var mapView = hud.GetComponent<MinimapView>();
			mapView.enabled = false;
			mapView.UpdateBlips(new[] {
				new MinimapBlip(Vector2.zero, Color.white),
				new MinimapBlip(Vector2.one, Color.yellow, true, true),
				new MinimapBlip(Vector2.one * 0.5f, Color.red, true, false, true)
			});
			Assert.That(map[0].style.left.value.value, Is.GreaterThanOrEqualTo(0));
			Assert.That(map[0].style.top.value.value + map[0].style.height.value.value,
				Is.LessThanOrEqualTo(map.contentRect.height + 0.1f));
			Assert.That(map[0].style.top.value.value, Is.GreaterThan(map.contentRect.height * 0.8f));
			Assert.That(map[1].style.left.value.value, Is.GreaterThan(map.contentRect.width * 0.8f));
			Assert.That(map[1].ClassListContains("minimap-blip--local"), Is.True);
			Assert.That(map[1].style.left.value.value + map[1].style.width.value.value,
				Is.LessThanOrEqualTo(map.contentRect.width + 0.1f));
			System.IO.Directory.CreateDirectory("Temp/FeelAudit");
			ScreenCapture.CaptureScreenshot("Temp/FeelAudit/Hud.png");
			yield return null;
			yield return null;
			mapView.enabled = true;
		}

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
            FoodRecord[] foods = AllFood();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            FoodRecord target = foods.OrderBy(food => (food.Position - (Vector2)player.transform.position).sqrMagnitude).First();
            player.transform.position = (Vector3)target.Position + Vector3.left * 2f;
            player.GetComponent<Rigidbody2D>().position = player.transform.position;
            float start = Time.time;
            float oldScale = Time.timeScale;
            float oldMaximum = Time.maximumDeltaTime;
            float[] thresholds = { 0.35f, 0.8f, 1.4f, 2f, 4f, 8f };
            float[] reached = { -1f, -1f, -1f, -1f, -1f, -1f };
            int[] sampleTimes = { 60, 180, 240, 360, 480, 600 };
            int sample = 0;
            float lastDiagnostic = -20f;
            float lastGrowthMass = player.Capacity;
            float lastGrowthTime = 0f;
            var skippedUntil = new System.Collections.Generic.Dictionary<FoodRecord, float>();
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
						System.IO.Directory.CreateDirectory("Logs");
						System.IO.File.WriteAllText("Logs/GrowthPace.txt", report.ToString());
                    }
                    if (player.Capacity > lastGrowthMass + 0.001f)
                    {
                        lastGrowthMass = player.Capacity;
                        lastGrowthTime = elapsed;
                    }
                    else if (elapsed - lastGrowthTime >= 15f)
                    {
                        skippedUntil[target] = elapsed + 60f;
                        lastGrowthTime = elapsed;
                        report.AppendLine($"stuck t={elapsed:F0}s mass={player.Capacity:F4} position={player.transform.position} skipped target={target.Position} " +
                                          $"distance={Vector2.Distance(player.transform.position, target.Position):F3} bodyRadius={player.BodyRadius:F3} " +
                                          $"active={target.Active} dropped={target.IsDropped} " +
                                          $"immune={target.IsMagnetImmuneFor(player.NetworkObjectId)} " +
                                          $"edge={WorldBounds.DistanceToEdge(player.transform.position):F1} " +
                                          $"claimed={typeof(FoodRecord).GetField("Claimed", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target)} " +
                                          $"spawned={target.Active} version={target.LifecycleVersion}");
                    }
                    target = foods.Where(food => !food.IsDropped && food.Active &&
                                                 !(skippedUntil.TryGetValue(food, out float until) && until > elapsed))
                        .OrderBy(food => (food.Position - (Vector2)player.transform.position).sqrMagnitude).First();
                    movement.SetDirection(target.Position - (Vector2)player.transform.position);
                    if (elapsed - lastDiagnostic >= 20f)
                    {
                        lastDiagnostic = elapsed;
                        int candidates = foods.Count(food => !food.IsDropped && food.Active);
                        int sceneFood = AllFood().Length;
                        report.AppendLine($"diag t={elapsed:F0}s mass={player.Capacity:F4} position={player.transform.position} " +
                                          $"target={target.Position} distance={Vector2.Distance(player.transform.position, target.Position):F2} " +
                                          $"candidates={candidates} sceneFood={sceneFood} speed={player.GetComponent<Rigidbody2D>().linearVelocity.magnitude:F2}");
                    }
                    SetMoonField(player, "_pendingOffers", 0);
                    SetMoonField(player, "_offerCount", 0);
                    yield return new WaitForFixedUpdate();
                }
                report.AppendLine($"End: t={Time.time - start:F1}s mass={player.Capacity:F4}; defeated={player.IsDefeated}");
                for (int i = 0; i < thresholds.Length; i++) report.AppendLine($"mass={thresholds[i]:F2} reached={reached[i]:F1}s (-1 = not reached)");
				System.IO.Directory.CreateDirectory("Logs");
				System.IO.File.WriteAllText("Logs/GrowthPace.txt", report.ToString());
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

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Growth_MassNeverChangesActualBaseOrBoostVelocity()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            MethodInfo step = typeof(PlayerMovement).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (float mass in new[] { 0.1f, 0.35f, 0.8f, 2f, 4f, 8f })
            {
                player.Capacity = mass;
                Assert.That(player.Capacity, Is.EqualTo(mass));
                player.SetBoostRpc(false);
                step.Invoke(movement, null);
				Assert.That(body.linearVelocity.magnitude, Is.EqualTo(MovementRules.PlayerSpeed).Within(0.001f));
                typeof(PlayerMovement).GetField("_isBoosting", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(movement, true);
                player.SetBoostRpc(true);
                step.Invoke(movement, null);
				Assert.That(body.linearVelocity.magnitude, Is.EqualTo(MovementRules.PlayerBoostSpeed).Within(0.001f));
            }
            GrantPerk(player, PerkId.IonTrail);
            step.Invoke(movement, null);
			Assert.That(body.linearVelocity.magnitude, Is.EqualTo(MovementRules.PlayerBoostSpeed * 1.15f).Within(0.001f));
            player.SetBoostRpc(false);
            step.Invoke(movement, null);
			Assert.That(body.linearVelocity.magnitude, Is.EqualTo(MovementRules.PlayerSpeed).Within(0.001f));
            GrantPerk(player, PerkId.SolarSail);
            step.Invoke(movement, null);
			Assert.That(body.linearVelocity.magnitude, Is.EqualTo(MovementRules.PlayerSpeed * 1.12f).Within(0.001f));
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
            float previousMaximumDiameter = 0f;
            foreach (float mass in new[] { 0.081f, 2f, 8f })
            {
                player.Capacity = mass;
                yield return new WaitForSeconds(mass < 1f ? 1.5f : 3.5f);
                float diameter = player.GetComponent<SpriteRenderer>().bounds.size.x / (2f * camera.orthographicSize);
                if (baseline == 0f) baseline = diameter;
                Debug.Log($"Growth view: mass={mass:F2}, zoom={camera.orthographicSize:F3}, relative diameter={diameter / baseline:F3}");
                if (mass == 2f)
                {
                    Assert.That(diameter / baseline, Is.GreaterThan(2.7f));
                    previousMaximumDiameter = diameter;
                }
                if (mass == 8f)
                {
                    Assert.That(player.Capacity, Is.EqualTo(8f));
                    Assert.That(diameter, Is.GreaterThan(previousMaximumDiameter));
                }
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
