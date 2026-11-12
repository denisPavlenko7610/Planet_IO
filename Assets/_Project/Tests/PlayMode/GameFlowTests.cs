using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlanetIO.Bootstrap;
using PlanetIO.Infrastructure.Networking;
using PlanetIO.UI.Hud;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VContainer;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class GameFlowTests
    {
        [UnityTest]
        [Timeout(45000)]
        public IEnumerator BootToSoloGameAndBackToMenu_Completes()
        {
            yield return OpenMenu();

            Button soloButton = FindButton("play-with-bots");
            Assert.That(soloButton, Is.Not.Null);
            Click(soloButton);

            yield return WaitForScene(SceneNames.Game, 18f);
            yield return WaitForOpponentCount(10, 5f);
            yield return WaitForMusicClip("GameTheme", 5f);

            Assert.That(GameObject.Find("SessionHud"), Is.Not.Null);
            Assert.That(
                Object.FindObjectsByType<Enemy>(
                    FindObjectsInactive.Exclude).Length,
                Is.GreaterThanOrEqualTo(10));

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);
            localPlayer.Defeat();

            yield return WaitForDefeatView(2f);
            Assert.That(localPlayer.IsDefeated, Is.True);

            Button leaveButton = FindButton("leave-button");
            Assert.That(leaveButton, Is.Not.Null);
            Click(leaveButton);

            yield return WaitForScene(SceneNames.Menu, 10f);
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator LocalPlayer_UsesPreferredColorAfterSpawnProtection()
        {
            yield return StartSoloGame();

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);

            yield return new WaitForSeconds(2.5f);

            Color expected = ResolveProfile().PreferredColor;
            Color actual = localPlayer.GetComponent<SpriteRenderer>().color;
            Assert.That(ColorDistance(expected, actual), Is.LessThan(0.02f),
                $"Expected {expected}, got {actual}.");

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator Continue_AfterStarving_RevivesAboveMinimumCapacity()
        {
            yield return StartSoloGame();

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);

            localPlayer.Capacity = 0f;
            yield return WaitForDefeatView(2f);
            Assert.That(localPlayer.IsDefeated, Is.True);

            Button continueButton = FindButton("continue-button");
            Assert.That(continueButton, Is.Not.Null);
            Click(continueButton);

            float deadline = Time.realtimeSinceStartup + 3f;
            while (localPlayer.IsDefeated && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(localPlayer.IsDefeated, Is.False);
            Assert.That(localPlayer.Capacity, Is.GreaterThan(localPlayer.MinCapacity));
            Assert.That(localPlayer.IsSpawnProtected, Is.True);

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator PlayAgain_AfterBeingEaten_RespawnsInSameSession()
        {
            yield return StartSoloGame();

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);

            localPlayer.Defeat();
            yield return WaitForDefeatView(2f);

            Button playAgainButton = FindButton("play-again");
            Assert.That(playAgainButton, Is.Not.Null);
            Click(playAgainButton);

            float deadline = Time.realtimeSinceStartup + 3f;
            while (localPlayer.IsDefeated && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(localPlayer.IsDefeated, Is.False);
            Assert.That(localPlayer.IsSpawnProtected, Is.True);
            Assert.That(localPlayer.Capacity, Is.GreaterThan(localPlayer.MinCapacity));
            Assert.That(FindLocalPlayer(), Is.SameAs(localPlayer));

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator Defeat_DropsLootAndContinueRestoresConfiguredFraction()
        {
            yield return StartSoloGame();

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);

            localPlayer.Capacity = 1f;
            float capacity = localPlayer.Capacity;
            float droppedBefore = SumDroppedNutrition();

            localPlayer.Defeat();

            Assert.That(localPlayer.IsDefeated, Is.True);
            Assert.That(
                SumDroppedNutrition() - droppedBefore,
                Is.EqualTo(capacity * FoodSpawner.LootMassFraction).Within(1e-3f));

            float continueFraction = ReadPrivateField<float>(localPlayer, "_continueMassFraction");
            float initialCapacity = ReadPrivateField<float>(localPlayer, "_initialCapacity");
            localPlayer.ContinueRpc();

            Assert.That(localPlayer.IsDefeated, Is.False);
            Assert.That(
                localPlayer.Capacity,
                Is.EqualTo(Mathf.Max(initialCapacity, capacity * continueFraction)).Within(1e-4f));
            Assert.That(SumDroppedNutrition() - droppedBefore, Is.EqualTo(0f).Within(1e-3f));

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator PlayerEatsFood_WithAndWithoutSpawnProtection([Values(true, false)] bool spawnProtected)
        {
            yield return StartSoloGame();
            yield return WaitForGameplay(10f);

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);

            if (spawnProtected)
            {
                localPlayer.Defeat();
                localPlayer.ContinueRpc();
            }
            else
            {
                float protectionDeadline = Time.realtimeSinceStartup + 6f;
                while (localPlayer.IsSpawnProtected && Time.realtimeSinceStartup < protectionDeadline)
                {
                    yield return null;
                }
            }

            Assert.That(localPlayer.IsSpawnProtected, Is.EqualTo(spawnProtected));

            Food food = Object.FindObjectsByType<Food>(FindObjectsInactive.Exclude).First(item => !item.IsDropped);
            float capacityBefore = localPlayer.Capacity;

            TouchWith(localPlayer, food.GetComponent<Collider2D>());

            Assert.That(localPlayer.Capacity, Is.GreaterThan(capacityBefore));
            Assert.That(localPlayer.IsSpawnProtected, Is.EqualTo(spawnProtected));

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator CometHit_HurtsOnlyUnprotectedPlayer([Values(true, false)] bool spawnProtected)
        {
            yield return StartSoloGame();
            yield return WaitForGameplay(10f);

            Player localPlayer = FindLocalPlayer();
            Assert.That(localPlayer, Is.Not.Null);

            if (spawnProtected)
            {
                localPlayer.Defeat();
                localPlayer.ContinueRpc();
            }
            else
            {
                float protectionDeadline = Time.realtimeSinceStartup + 6f;
                while (localPlayer.IsSpawnProtected && Time.realtimeSinceStartup < protectionDeadline)
                {
                    yield return null;
                }
            }

            Assert.That(localPlayer.IsSpawnProtected, Is.EqualTo(spawnProtected));
            localPlayer.Capacity = 1f;

            Comet comet = Object.FindObjectsByType<Comet>(FindObjectsInactive.Exclude).First();
            float capacityBefore = localPlayer.Capacity;

            TouchWith(localPlayer, comet.GetComponent<Collider2D>());

            if (spawnProtected)
            {
                Assert.That(localPlayer.Capacity, Is.EqualTo(capacityBefore));
            }
            else
            {
                Assert.That(localPlayer.Capacity, Is.LessThan(capacityBefore));
            }

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator LeavingGame_ReleasesStaticPlayerState()
        {
            yield return StartSoloGame();

            IPlayerRegistry registry = Resolve<IPlayerRegistry>();
            Assert.That(registry.Count, Is.GreaterThan(0));
            Assert.That(registry.LocalPlayer, Is.Not.Null);

            yield return LeaveToMenu();
            yield return null;
            yield return null;

            registry = Resolve<IPlayerRegistry>();
            Assert.That(registry.Count, Is.Zero);
            Assert.That(registry.LocalPlayer, Is.Null);
            Assert.That(CountDefeatAnnouncementSubscribers(), Is.Zero);
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator RejectedSessionStart_DoesNotChangeActiveRoom()
        {
            yield return StartSoloGame();

            INetworkSessionService session = Resolve<INetworkSessionService>();
            RoomConnectionSettings activeRoom = session.CurrentRoom;
            NetworkSessionMode activeMode = session.Mode;
            bool started = true;

            yield return Await(session.StartClientAsync("ABCD"), result => started = result);

            Assert.That(started, Is.False);
            Assert.That(session.CurrentRoom, Is.EqualTo(activeRoom));
            Assert.That(session.Mode, Is.EqualTo(activeMode));

            yield return LeaveToMenu();
        }

        [UnityTest]
        [Timeout(45000)]
        public IEnumerator RoomApproval_RejectsClientWhenRoomIsFull()
        {
            yield return StartSoloGame();

            ConnectionApprovalHandler handler = new(Resolve<NetworkManager>());
            byte[] payload = ConnectionApprovalHandler.SerializePayload(new ConnectionApprovalHandler.RoomConnectionPayload
            {
                Protocol = RoomRules.ProtocolVersion,
                Nickname = "Guest"
            });
            NetworkManager.ConnectionApprovalRequest request = new() { ClientNetworkId = 99, Payload = payload };

            NetworkManager.ConnectionApprovalResponse full = new();
            handler.ApproveRoomConnection(request, full, new RoomConnectionSettings("ROOM", 1));
            NetworkManager.ConnectionApprovalResponse spare = new();
            handler.ApproveRoomConnection(request, spare, new RoomConnectionSettings("ROOM", 2));

            Assert.That(full.Approved, Is.False);
            Assert.That(full.Reason, Is.EqualTo(SessionFailureReasons.RoomFull));
            Assert.That(spare.Approved, Is.True);

            yield return LeaveToMenu();
        }

        private static IEnumerator StartSoloGame()
        {
            yield return OpenMenu();

            Button soloButton = FindButton("play-with-bots");
            Assert.That(soloButton, Is.Not.Null);
            Click(soloButton);

            yield return WaitForScene(SceneNames.Game, 18f);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (FindLocalPlayer() == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        [UnityTearDown]
        public IEnumerator StopSession()
        {
            ApplicationLifetimeScope scope = Object.FindAnyObjectByType<ApplicationLifetimeScope>();
            if (scope == null || scope.Container == null)
			{
				yield break;
			}
			var operation = scope.Container.Resolve<INetworkSessionService>().ShutdownAndReturnToMenuAsync();
            var awaiter = operation.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;
            awaiter.GetResult();
        }

        private static IEnumerator OpenMenu()
        {
            bool applicationStarted = Object.FindAnyObjectByType<ApplicationLifetimeScope>() != null;
            SceneManager.LoadScene(applicationStarted ? SceneNames.Menu : SceneNames.Boot);
            yield return WaitForScene(SceneNames.Menu, 12f);
        }

        private static IEnumerator LeaveToMenu()
        {
            Button leaveButton = FindButton("leave-button");
            Assert.That(leaveButton, Is.Not.Null);
            Click(leaveButton);

            yield return WaitForScene(SceneNames.Menu, 10f);
        }

        private static IPlayerProfileService ResolveProfile() => Resolve<IPlayerProfileService>();

        private static T Resolve<T>() =>
            Object.FindAnyObjectByType<ApplicationLifetimeScope>().Container.Resolve<T>();

        private static IEnumerator Await(Awaitable<bool> operation, Action<bool> onResult)
        {
            var awaiter = operation.GetAwaiter();
            while (!awaiter.IsCompleted)
            {
                yield return null;
            }

            onResult(awaiter.GetResult());
        }

        private static IEnumerator WaitForGameplay(float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                GameLifetimeScope scope = Object.FindAnyObjectByType<GameLifetimeScope>();
                if (scope != null && scope.Container != null && scope.Container.Resolve<IGameStateService>().IsGameplayActive)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Gameplay did not become active in time.");
        }

        private static void TouchWith(Player player, Collider2D other)
        {
            MethodInfo method = typeof(Player).GetMethod(
                "OnTriggerEnter2D", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(method, Is.Not.Null, "Player.OnTriggerEnter2D");
            method.Invoke(player, new object[] { other });
        }

        private static float SumDroppedNutrition()
        {
            float total = 0f;
            foreach (Food food in Object.FindObjectsByType<Food>(FindObjectsInactive.Exclude))
            {
                if (food.IsDropped)
                {
                    total += food.Nutrition;
                }
            }

            return total;
        }

        private static T ReadPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, fieldName);
            return (T)field.GetValue(target);
        }

        private static int CountDefeatAnnouncementSubscribers()
        {
            FieldInfo field = typeof(Player).GetField(
                "DefeatAnnounced", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, "Player.DefeatAnnounced backing field");
            return (field.GetValue(null) as Delegate)?.GetInvocationList().Length ?? 0;
        }

        private static float ColorDistance(Color left, Color right) =>
            Mathf.Abs(left.r - right.r) + Mathf.Abs(left.g - right.g) + Mathf.Abs(left.b - right.b);

        private static IEnumerator WaitForScene(
            string sceneName,
            float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (SceneManager.GetActiveScene().name != sceneName &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetActiveScene().name,
                Is.EqualTo(sceneName),
                $"Scene '{sceneName}' was not loaded in time.");
        }

        private static IEnumerator WaitForOpponentCount(
            int minimumCount,
            float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (CountOpponents() < minimumCount &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitForMusicClip(
            string expectedClipName,
            float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            AudioSource musicSource = null;

            while (Time.realtimeSinceStartup < deadline)
            {
                GameObject musicObject =
                    GameObject.Find("Addressable Music");
                musicSource = musicObject != null
                    ? musicObject.GetComponent<AudioSource>()
                    : null;

                if (musicSource?.clip != null &&
                    musicSource.clip.name == expectedClipName)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(musicSource, Is.Not.Null);
            Assert.That(musicSource.clip, Is.Not.Null);
            Assert.That(
                musicSource.clip.name,
                Is.EqualTo(expectedClipName));
        }

        private static int CountOpponents() =>
            Object.FindObjectsByType<Enemy>(
                FindObjectsInactive.Exclude).Length;

        private static Player FindLocalPlayer() =>
            Object.FindObjectsByType<Player>(
                    FindObjectsInactive.Exclude)
                .FirstOrDefault(player => player.IsOwner);

        private static IEnumerator WaitForDefeatView(
            float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                SessionHudView view =
                    Object.FindAnyObjectByType<SessionHudView>(
                        FindObjectsInactive.Exclude);
                if (view != null && view.IsDefeatVisible)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Defeat UI was not shown in time.");
        }

        private static Button FindButton(string elementName)
        {
            foreach (UIDocument document in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
            {
                Button button = document.rootVisualElement?.Q<Button>(elementName);
                if (button != null)
                {
                    return button;
                }
            }

            return null;
        }

        private static void Click(Button button)
        {
            using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
            submit.target = button;
            button.SendEvent(submit);
        }
    }
}