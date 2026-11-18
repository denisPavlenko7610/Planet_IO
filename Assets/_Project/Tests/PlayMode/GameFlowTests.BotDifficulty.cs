using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using RD.Settings;
using PlanetIO.UI.Settings;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, Timeout(60000)]
        public IEnumerator Bots_DifficultyChangesSoloDecisionsAndBoostSpendsMass()
        {
            yield return StartMoonGame();
            ISettingsService settings = Resolve<ISettingsService>();
            int previous = settings.Get(GameSettingKeys.BotDifficulty);
            try
            {
                Player player = PrepareMoonPlayer();
                player.Capacity = 0.2f;
                player.GetComponent<PlayerMovement>().enabled = false;
                player.GetComponent<PlanetGravity>().enabled = false;
                player.transform.position = Vector2.right * 5f;
                player.GetComponent<Rigidbody2D>().position = player.transform.position;
                Enemy bot = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
                foreach (Enemy other in Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
                {
                    other.GetComponent<EnemyMovement>().enabled = false;
                    other.GetComponent<PlanetGravity>().enabled = false;
                    if (other != bot) other.GetComponent<Collider2D>().enabled = false;
                }
                foreach (Comet comet in Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude))
                    comet.GetComponent<Collider2D>().enabled = false;
                EnemyMovement movement = bot.GetComponent<EnemyMovement>();
                bot.transform.position = Vector2.zero;
                bot.GetComponent<Rigidbody2D>().position = Vector2.zero;
                Physics2D.SyncTransforms();
                foreach (int difficulty in new[] { 0, 1, 2 })
                {
                    settings.Set(GameSettingKeys.BotDifficulty, difficulty);
                    movement.ResetForRespawn();
                    bot.Capacity = 1f;
                    typeof(EnemyMovement).GetProperty("Direction").SetValue(movement, Vector2.right);
                    InvokeBot(movement, "Think");
                    Assert.That((int)movement.Difficulty, Is.EqualTo(difficulty));
                    Assert.That(movement.State, Is.EqualTo(EnemyMovement.MovementState.Hunting));
                    movement.Move();
                    float normalSpeed = bot.GetComponent<Rigidbody2D>().linearVelocity.magnitude;
                    InvokeBot(movement, "UpdateBoost", 0.02f);
                    movement.Move();
                    Assert.That(bot.IsBoosting, Is.EqualTo(difficulty != 0));
                    if (difficulty != 0)
                    {
                        Assert.That(bot.Capacity, Is.EqualTo(1f - BotDifficultyRules.BoostCost).Within(0.00001f));
                        Assert.That(bot.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
                            Is.EqualTo(normalSpeed * 2f).Within(0.001f));
                        Assert.That(AllFood().Any(food =>
                            food.IsDropped && Mathf.Abs(food.Nutrition - BotDifficultyRules.BoostCost * BotDifficultyRules.BoostFoodFraction) < 0.00001f), Is.True);
                    }
                    else Assert.That(bot.Capacity, Is.EqualTo(1f));
                    if (difficulty != 0)
                    {
                        InvokeBot(movement, "StopBoost");
                        float remainingMass = bot.Capacity;
                        InvokeBot(movement, "UpdateBoost", 0.02f);
                        Assert.That(bot.IsBoosting, Is.False, "Cooldown prevents an immediate second burst.");
                        Assert.That(bot.Capacity, Is.EqualTo(remainingMass));
                    }
                    InvokeBot(movement, "StopBoost");
                    bot.Capacity = 8f;
                    movement.Move();
                    Assert.That(bot.GetComponent<Rigidbody2D>().linearVelocity.magnitude,
                        Is.EqualTo(normalSpeed).Within(0.001f), "Mass never increases base speed.");
                    bot.Capacity = bot.InitialCapacity;
                    InvokeBot(movement, "UpdateBoost", 0.02f);
                    Assert.That(bot.IsBoosting, Is.False);
                    Assert.That(bot.Capacity, Is.EqualTo(bot.InitialCapacity));
                    movement.ResetForRespawn();
                    Assert.That(bot.IsBoosting, Is.False);
                    bot.Capacity = 1f;
                    player.Capacity = 2f;
                    InvokeBot(movement, "Think");
                    Assert.That(movement.State, Is.EqualTo(EnemyMovement.MovementState.Evading));
                    typeof(EnemyMovement).GetProperty("Direction").SetValue(movement,
                        typeof(EnemyMovement).GetField("_desiredDirection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(movement));
                    InvokeBot(movement, "UpdateBoost", 0.02f);
                    Assert.That(bot.IsBoosting, Is.True, "Every difficulty can boost defensively.");
                    movement.ResetForRespawn();
                    player.Capacity = 0.2f;
                }
                Enemy threat = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First(other => other != bot);
                threat.Capacity = 1.1f;
                threat.transform.position = Vector2.right * 7f;
                threat.GetComponent<Rigidbody2D>().position = threat.transform.position;
                threat.GetComponent<Collider2D>().enabled = true;
                bot.Capacity = 1f;
                Physics2D.SyncTransforms();
                InvokeBot(movement, "Think");
                Assert.That(movement.State, Is.EqualTo(EnemyMovement.MovementState.Evading));
                Assert.That(typeof(EnemyMovement).GetField("_targetPlanet", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(movement), Is.SameAs(threat), "A lethal bot takes priority over closer edible prey.");
                threat.GetComponent<Collider2D>().enabled = false;
                SetMoonField(player, "_invincibilityTimeRemaining", 1000f);
                Physics2D.SyncTransforms();
                InvokeBot(movement, "Think");
                Assert.That(typeof(EnemyMovement).GetField("_targetPlanet", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(movement), Is.Null, "Spawn-protected players are not targets.");
            }
            finally
            {
                settings.Set(GameSettingKeys.BotDifficulty, previous);
                settings.Flush();
            }
        }

        [UnityTest, Timeout(45000)]
        public IEnumerator Settings_BotDifficultyCyclesAndSurvivesSceneChange()
        {
            yield return OpenMenu();
            ISettingsService settings = Resolve<ISettingsService>();
            int previous = settings.Get(GameSettingKeys.BotDifficulty);
            try
            {
                settings.Reset(GameSettingKeys.BotDifficulty);
                Click(FindButton("settings-button"));
                yield return null;
                SettingsView view = Object.FindFirstObjectByType<SettingsView>();
                Assert.That(view.IsVisible, Is.True);
                VisualElement root = view.GetComponent<UIDocument>().rootVisualElement;
                Assert.That(settings.Get(GameSettingKeys.BotDifficulty), Is.EqualTo(1));
                Click(root.Q<Button>("next-difficulty"));
                Assert.That(settings.Get(GameSettingKeys.BotDifficulty), Is.EqualTo(2));
                Click(root.Q<Button>("next-difficulty"));
                Assert.That(settings.Get(GameSettingKeys.BotDifficulty), Is.EqualTo(0));
                Click(root.Q<Button>("previous-difficulty"));
                Assert.That(settings.Get(GameSettingKeys.BotDifficulty), Is.EqualTo(2));
                settings.ClearCache();
                Assert.That(settings.Get(GameSettingKeys.BotDifficulty), Is.EqualTo(2));
                ScreenCapture.CaptureScreenshot("Temp/BotDifficultySettings.png");
                yield return null;
                Click(root.Q<Button>("settings-close"));
                yield return StartSoloGame();
                yield return WaitForGameplay(8f);
                Assert.That(Object.FindObjectsByType<EnemyMovement>(FindObjectsInactive.Exclude)
                    .All(bot => bot.Difficulty == BotDifficulty.Hard), Is.True);
            }
            finally
            {
                settings.Set(GameSettingKeys.BotDifficulty, previous);
                settings.Flush();
            }
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Difficulty_SoloCometsMoonsAndBordersUseDamageTuning()
        {
            yield return StartSharedMoonGame();
            ISettingsService settings = Resolve<ISettingsService>();
            int previous = settings.Get(GameSettingKeys.BotDifficulty);
            try
            {
                Player player = PrepareMoonPlayer();
                Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
                Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
                foreach (int value in new[] { 0, 1, 2 })
                {
                    settings.Set(GameSettingKeys.BotDifficulty, value);
                    var difficulty = (BotDifficulty)value;
                    float playerScale = BotDifficultyRules.PlayerIncomingDamage(difficulty, NetworkSessionMode.SinglePlayer);
                    float botScale = BotDifficultyRules.BotIncomingDamage(difficulty);
                    player.Capacity = 1f;
                    TouchWith(player, comet.GetComponent<Collider2D>());
                    Assert.That(player.Capacity, Is.EqualTo(1f - 0.5f * playerScale).Within(0.00001f));
                    enemy.Capacity = 1f;
                    typeof(Enemy).GetMethod("OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(enemy, new object[] { comet.GetComponent<Collider2D>() });
                    Assert.That(enemy.Capacity, Is.EqualTo(1f - 0.5f * botScale).Within(0.00001f));
                    player.Capacity = 1f;
                    typeof(Player).GetMethod("OnBorderHit", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(player, new object[] { player });
                    Assert.That(player.Capacity, Is.EqualTo(1f - 0.08f * playerScale).Within(0.00001f));
                    player.Capacity = 1f;
                    SetMoonField(player, "_moonImmunityRemaining", 0f);
                    object[] attack = { enemy, Vector2.left, BotDifficultyRules.BotOutgoingDamage(difficulty), 0f };
                    typeof(PlanetScale).GetMethod("TryReceiveStrike", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(player, attack);
                    Assert.That(player.Capacity, Is.EqualTo(1f - 0.10f * BotDifficultyRules.BotOutgoingDamage(difficulty) * playerScale).Within(0.00001f));
                    enemy.Capacity = 1f;
                    SetMoonField(enemy, "_moonImmunityRemaining", 0f);
                    object[] retaliation = { player, Vector2.left, 1f, 0f };
                    typeof(PlanetScale).GetMethod("TryReceiveStrike", BindingFlags.Instance | BindingFlags.NonPublic)
                        .Invoke(enemy, retaliation);
                    Assert.That(enemy.Capacity, Is.EqualTo(1f - 0.10f * botScale).Within(0.00001f));
                }
            }
            finally
            {
                settings.Set(GameSettingKeys.BotDifficulty, previous);
                settings.Flush();
            }
        }

        [UnityTest, SharedWorld, Timeout(45000)]
        public IEnumerator Loot_PlayerAndEatenBotLeaveFoodAtDeathLocation()
        {
            yield return StartSharedMoonGame();
            Player player = PrepareMoonPlayer();
            player.Capacity = 1f;
            Enemy enemy = Object.FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).First();
            enemy.Capacity = 0.35f;
            Vector2 deathPosition = enemy.transform.position;
            var before = AllFood()
                .Where(food => food.IsDropped).ToHashSet();
            TouchWith(player, enemy.GetComponent<Collider2D>());
            AssertDeathFood(before, deathPosition, 0.35f);
            Assert.That(enemy.Capacity, Is.EqualTo(enemy.InitialCapacity));
            before = AllFood()
                .Where(food => food.IsDropped).ToHashSet();
            player.Capacity = 1f;
            deathPosition = player.transform.position;
            player.Defeat();
            AssertDeathFood(before, deathPosition, 1f);
        }

        private static void AssertDeathFood(HashSet<FoodRecord> before, Vector2 position, float mass)
        {
            FoodRecord[] food = AllFood()
                .Where(point => point.IsDropped && !before.Contains(point)).ToArray();
            Assert.That(food.Length, Is.InRange(4, 30));
            Assert.That(food.Sum(point => point.Nutrition), Is.EqualTo(mass * 0.6f).Within(0.00001f));
            Assert.That(food.All(point => Vector2.Distance(point.Position, position)
                <= Mathf.Max(mass * 1.5f, 0.6f) * 1.21f), Is.True);
        }

        private static void InvokeBot(EnemyMovement movement, string method, params object[] args) =>
            typeof(EnemyMovement).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(movement, args);
    }
}
