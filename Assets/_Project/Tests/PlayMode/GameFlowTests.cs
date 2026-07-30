using System.Collections;
using System.Linq;
using NUnit.Framework;
using PlanetIO.UI.Hud;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed class GameFlowTests
    {
        [UnityTest]
        [Timeout(45000)]
        public IEnumerator BootToSoloGameAndBackToMenu_Completes()
        {
            SceneManager.LoadScene(SceneNames.Boot);
            yield return WaitForScene(SceneNames.Menu, 12f);

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
