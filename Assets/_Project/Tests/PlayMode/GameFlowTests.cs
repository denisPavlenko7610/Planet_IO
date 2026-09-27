using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlanetIO.Infrastructure.Bootstrap;
using PlanetIO.UI.Hud;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
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

            Button soloButton = FindButton("SinglePlayerButton");
            Assert.That(soloButton, Is.Not.Null);
            soloButton.onClick.Invoke();

            yield return WaitForScene(SceneNames.Game, 18f);
            yield return WaitForOpponentCount(10, 5f);
            yield return WaitForMusicClip("Map", 5f);

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

            Button leaveButton = FindButton("LeaveButton");
            Assert.That(leaveButton, Is.Not.Null);
            leaveButton.onClick.Invoke();

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

            Button continueButton = FindButton("ContinueButton");
            Assert.That(continueButton, Is.Not.Null);
            continueButton.onClick.Invoke();

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

            Button playAgainButton = FindButton("PlayAgainButton");
            Assert.That(playAgainButton, Is.Not.Null);
            playAgainButton.onClick.Invoke();

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

        private static IEnumerator StartSoloGame()
        {
            yield return OpenMenu();

            Button soloButton = FindButton("SinglePlayerButton");
            Assert.That(soloButton, Is.Not.Null);
            soloButton.onClick.Invoke();

            yield return WaitForScene(SceneNames.Game, 18f);

            float deadline = Time.realtimeSinceStartup + 5f;
            while (FindLocalPlayer() == null && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator OpenMenu()
        {
            bool applicationStarted = Object.FindAnyObjectByType<ApplicationLifetimeScope>() != null;
            SceneManager.LoadScene(applicationStarted ? SceneNames.Menu : SceneNames.Boot);
            yield return WaitForScene(SceneNames.Menu, 12f);
        }

        private static IEnumerator LeaveToMenu()
        {
            Button leaveButton = FindButton("LeaveButton");
            Assert.That(leaveButton, Is.Not.Null);
            leaveButton.onClick.Invoke();

            yield return WaitForScene(SceneNames.Menu, 10f);
        }

        private static IPlayerProfileService ResolveProfile() =>
            Object.FindAnyObjectByType<ApplicationLifetimeScope>()
                .Container.Resolve<IPlayerProfileService>();

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

        private static Button FindButton(string objectName) =>
            Object.FindObjectsByType<Button>(
                    FindObjectsInactive.Exclude)
                .FirstOrDefault(button => button.name == objectName);
    }
}
