using System.Collections;
using NUnit.Framework;
using PlanetIO.Bootstrap;
using RD.Settings;
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
		public IEnumerator Progress_TrackEasyExposureUntilTheNextRun()
		{
			yield return StartMoonGame();

			ISettingsService settings = Resolve<ISettingsService>();
			IRunStats stats = ResolveGame<IRunStats>();
			int previousDifficulty = settings.Get(GameSettingKeys.BotDifficulty);
			try
			{
				settings.Set(GameSettingKeys.BotDifficulty, (int)BotDifficulty.Medium);
				stats.BeginRun();
				long runId = stats.Current.RunId;
				Assert.That(stats.Current.UsedEasyDifficulty, Is.False);

				settings.Set(GameSettingKeys.BotDifficulty, (int)BotDifficulty.Easy);
				yield return null;

				settings.Set(GameSettingKeys.BotDifficulty, (int)BotDifficulty.Medium);
				yield return null;

				Assert.That(stats.Current.RunId, Is.EqualTo(runId));
				Assert.That(stats.Current.UsedEasyDifficulty, Is.True);
				stats.BeginRun();
				Assert.That(stats.Current.UsedEasyDifficulty, Is.False);
			}
			finally
			{
				settings.Set(GameSettingKeys.BotDifficulty, previousDifficulty);
			}
		}

        [UnityTest, Timeout(60000)]
        public IEnumerator Progress_DefeatAwardsRunXpOnceAndShowsTheResult()
        {
            yield return StartMoonGame();
            IProgressionService progression = Resolve<IProgressionService>();
            IRunStats stats = ResolveGame<IRunStats>();
            Player player = PrepareMoonPlayer();
            int xpBefore = progression.Xp;
            int dustBefore = progression.Dust;
            player.Capacity = 0.5f;
            yield return new WaitForSeconds(1.2f);

            player.Defeat();
            yield return WaitForDefeatView(3f);

            RunSummary summary = stats.Current;
            Assert.That(summary.DurationSeconds, Is.GreaterThan(1f));
            Assert.That(summary.PeakTier, Is.GreaterThanOrEqualTo(2));
            Assert.That(progression.Xp - xpBefore, Is.EqualTo(XpRules.RunXp(summary)));
            Assert.That(progression.Dust, Is.EqualTo(dustBefore + DustRules.RunDust(XpRules.RunXp(summary))));
            Assert.That(FindLabel("result-details").text, Is.Not.Empty);
            Assert.That(FindLabel("rank-text").text, Is.Not.Empty);
            Assert.That(FindLabel("reward-text").text, Does.Contain(XpRules.RunXp(summary).ToString()));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PlatformServices_DefeatCountsTheRunOnceAndEveryServiceResolves()
        {
            yield return StartMoonGame();
            IServiceStateService state = Resolve<IServiceStateService>();
            Assert.That(state.IsLoaded, Is.True);
            Assert.That(Resolve<IPostRunService>(), Is.Not.Null);
            Assert.That(Resolve<ICloudSyncService>(), Is.Not.Null);
            Assert.That(Resolve<IPlayGamesService>(), Is.Not.Null);
            Assert.That(Resolve<IReviewService>(), Is.Not.Null);
            Assert.That(System.Enum.IsDefined(typeof(BotDifficulty), Resolve<IRunDifficultyProvider>().Current), Is.True);
            int runsBefore = state.State.RunsFinished;
            Player player = PrepareMoonPlayer();
            player.Capacity = 0.5f;
            yield return new WaitForSeconds(1.2f);

            player.Defeat();
            yield return WaitForDefeatView(3f);

            Assert.That(state.State.RunsFinished, Is.EqualTo(runsBefore + 1));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Progress_ContinueThenSecondDefeatPaysOnlyTheDifference()
        {
            yield return StartMoonGame();
            IProgressionService progression = Resolve<IProgressionService>();
            IRunStats stats = ResolveGame<IRunStats>();
            Player player = PrepareMoonPlayer();
            int xpBefore = progression.Xp;
            player.Capacity = 0.5f;
            yield return new WaitForSeconds(0.6f);

            player.Defeat();
            yield return WaitForDefeatView(3f);
            int afterFirst = progression.Xp;
            player.ContinueRpc();
            player.Capacity = 1.2f;
            yield return new WaitForSeconds(0.8f);
            SetMoonField(player, "_invincibilityTimeRemaining", 0f);
            player.Defeat();
            yield return WaitForDefeatView(3f);

            Assert.That(afterFirst, Is.GreaterThan(xpBefore));
            Assert.That(progression.Xp, Is.GreaterThan(afterFirst), "A longer run pays the extra experience.");
            Assert.That(progression.Xp - xpBefore, Is.EqualTo(XpRules.RunXp(stats.Current)), "No double counting.");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Progress_PlayAgainStartsAFreshRun()
        {
            yield return StartMoonGame();
            IProgressionService progression = Resolve<IProgressionService>();
            IRunStats stats = ResolveGame<IRunStats>();
            Player player = PrepareMoonPlayer();
            player.Capacity = 0.5f;
            yield return new WaitForSeconds(0.6f);
            player.Defeat();
            yield return WaitForDefeatView(3f);
            long firstRun = stats.Current.RunId;
            int afterFirst = progression.Xp;

            Click(FindButton("play-again"));
            yield return null;

            Assert.That(stats.Current.RunId, Is.Not.EqualTo(firstRun));
            Assert.That(stats.Current.Kills, Is.Zero);
            Assert.That(stats.Current.DurationSeconds, Is.LessThan(0.5f));

            player = FindLocalPlayer();
            SetMoonField(player, "_invincibilityTimeRemaining", 0f);
            player.Capacity = 0.5f;
            yield return new WaitForSeconds(0.6f);
            player.Defeat();
            yield return WaitForDefeatView(3f);

            Assert.That(progression.Xp - afterFirst, Is.EqualTo(XpRules.RunXp(stats.Current)));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Progress_TrackerCountsKillsPerksAndTier()
        {
            yield return StartMoonGame();
            IRunStats stats = ResolveGame<IRunStats>();
            Player player = PrepareMoonPlayer();
            player.Capacity = 0.1f;

            player.Capacity = 0.9f;
            yield return null;

            Assert.That(stats.Current.PeakTier, Is.GreaterThanOrEqualTo(3));
            Assert.That(stats.Current.PeakScore, Is.GreaterThanOrEqualTo(90));
            GrantPerk(player, PerkId.Predator);
            yield return WaitUntil(() => stats.Current.PerksPicked >= 1, 2f);
            Assert.That(stats.Current.PerksPicked, Is.EqualTo(1));
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator Progress_MenuHidesRankAndShowsVersionAndMissions()
        {
            yield return OpenMenu();
            IProgressionService progression = Resolve<IProgressionService>();
            yield return null;

			Assert.That(FindLabel("rank-dust"), Is.Null);
			Assert.That(FindLabel("version-text").text, Is.EqualTo("v" + UnityEngine.Application.version));
            Assert.That(progression.Missions.Count, Is.EqualTo(MissionRules.DailyCount));
            VisualElement overlay = FindElement("missions-overlay");
            Assert.That(overlay.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            Click(FindButton("missions-button"));
            yield return null;

            Assert.That(overlay.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(FindLabel("missions-title").text, Is.Not.Empty);
            for (int index = 0; index < MissionRules.DailyCount; index++)
            {
                Assert.That(FindLabel($"mission-name-{index}").text, Is.Not.Empty);
                Assert.That(FindLabel($"mission-progress-{index}").text, Does.Contain("/"));
            }

            Click(FindButton("missions-close"));
            yield return null;

            Assert.That(overlay.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

		[UnityTest, Timeout(60000)]
		public IEnumerator Progress_BrowsingThenBuyingALockedSkinSpendsDustOnce()
		{
			yield return OpenMenu();

			IProgressionService progression = Resolve<IProgressionService>();
			IPlayerProfileService profile = ResolveProfile();
			PlanetSkinCatalog catalog = Resolve<PlanetSkinCatalog>();
			int selectedBefore = profile.SelectedSkin;
			int steps = 1;
			while (steps < catalog.Count && profile.IsSkinUnlocked((selectedBefore + steps) % catalog.Count))
			{
				steps++;
			}

			Assert.That(steps, Is.LessThan(catalog.Count), "The test profile needs a locked skin.");
			int locked = (selectedBefore + steps) % catalog.Count;
			progression.AddDust(DustRules.SkinPrice);
			int dustBefore = progression.Dust;

			Click(FindButton("profile-button"));
			for (int step = 0; step < steps; step++)
			{
				Click(FindButton("skin-next"));
			}

			Assert.That(profile.IsSkinUnlocked(locked), Is.False);
			Assert.That(profile.SelectedSkin, Is.EqualTo(selectedBefore));
			Assert.That(progression.Dust, Is.EqualTo(dustBefore));

			Button buy = FindButton("skin-select");
			Assert.That(buy.enabledInHierarchy, Is.True);
			Click(buy);
			yield return null;

			Assert.That(profile.IsSkinUnlocked(locked), Is.True);
			Assert.That(profile.SelectedSkin, Is.EqualTo(locked));
			Assert.That(progression.Dust, Is.EqualTo(dustBefore - DustRules.SkinPrice));
			Assert.That(buy.enabledInHierarchy, Is.False);
		}

        private static T ResolveGame<T>() => Object.FindAnyObjectByType<GameLifetimeScope>().Container.Resolve<T>();

        private static Label FindLabel(string elementName)
        {
            foreach (UIDocument document in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
            {
                Label label = document.rootVisualElement?.Q<Label>(elementName);
                if (label != null)
                {
                    return label;
                }
            }

            return null;
        }

        private static VisualElement FindElement(string elementName)
        {
            foreach (UIDocument document in Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude))
            {
                VisualElement element = document.rootVisualElement?.Q<VisualElement>(elementName);
                if (element != null)
                {
                    return element;
                }
            }

            return null;
        }

    }
}
