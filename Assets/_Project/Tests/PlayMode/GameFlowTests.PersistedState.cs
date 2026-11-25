using System.Collections;
using NUnit.Framework;
using RD.Settings;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace PlanetIO.Tests
{
	public sealed partial class GameFlowTests
	{
		private bool _tutorialCompletedBeforeTests;

		[UnityOneTimeSetUp]
		public IEnumerator ResetPersistedGameplaySettings()
		{
			yield return OpenMenu();
			ISettingsService settings = Resolve<ISettingsService>();
			settings.Reset(GameSettingKeys.BotDifficulty);
			settings.Flush();
			_tutorialCompletedBeforeTests = settings.Get(GameSettingKeys.TutorialCompleted);
			settings.Set(GameSettingKeys.TutorialCompleted, true);
		}

		[UnityTest, Timeout(45000)]
		public IEnumerator Tutorial_BotGamePausesForMovementAndCometOnce()
		{
			yield return OpenMenu();
			ISettingsService settings = Resolve<ISettingsService>();
			string[] lessons = { "move", "food", "bot", "comet", "black-hole", "evolution", "boost", "rank" };
			int[] saved = new int[lessons.Length];

			for (int index = 0; index < lessons.Length; index++)
			{
				string key = "PlanetIO.Onboarding." + lessons[index];
				saved[index] = PlayerPrefs.GetInt(key, -1);
				PlayerPrefs.SetInt(key, lessons[index] == "move" || lessons[index] == "comet" ? 0 : 1);
			}

			settings.Set(GameSettingKeys.TutorialCompleted, false);
			try
			{
				yield return StartMoonGame();
				VisualElement overlay = FindElement("tutorial-overlay");
				yield return WaitUntil(() => overlay.style.display.value == DisplayStyle.Flex, 3f);
				Assert.That(overlay.style.display.value, Is.EqualTo(DisplayStyle.Flex));
				Assert.That(Time.timeScale, Is.Zero);
				double pausedAt = OrbitClock.Now;
				yield return new WaitForSecondsRealtime(0.25f);
				Assert.That(OrbitClock.Now, Is.EqualTo(pausedAt).Within(0.001));

				Click(FindButton("tutorial-continue"));
				Assert.That(Time.timeScale, Is.EqualTo(1f));
				Player player = FindLocalPlayer();
				Comet comet = Object.FindAnyObjectByType<Comet>();
				Assert.That(comet, Is.Not.Null);
				comet.transform.position = player.transform.position + Vector3.right;
				yield return WaitUntil(() => overlay.style.display.value == DisplayStyle.Flex, 3f);

				Assert.That(overlay.style.display.value, Is.EqualTo(DisplayStyle.Flex));
				string description = Resolve<RD.Localization.ILocalizationService>().Get(LocalizationKeys.TutorialComet);
				Assert.That(FindLabel("tutorial-description").text, Is.EqualTo(description));
				Assert.That(Time.timeScale, Is.Zero);

				Click(FindButton("tutorial-continue"));
				Assert.That(Time.timeScale, Is.EqualTo(1f));
				Assert.That(PlayerPrefs.GetInt("PlanetIO.Onboarding.comet"), Is.EqualTo(1));
				Assert.That(settings.Get(GameSettingKeys.TutorialCompleted), Is.True);
			}
			finally
			{
				settings.Set(GameSettingKeys.TutorialCompleted, true);
				for (int index = 0; index < lessons.Length; index++)
				{
					string key = "PlanetIO.Onboarding." + lessons[index];
					if (saved[index] == -1)
					{
						PlayerPrefs.DeleteKey(key);
					}
					else
					{
						PlayerPrefs.SetInt(key, saved[index]);
					}
				}

				PlayerPrefs.Save();
			}
		}
	}
}
