using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace PlanetIO.Tests
{
    public sealed class MenuStructureTests
    {
        private const string MenuPath = "Assets/_Project/UI/Menu.uxml";

        private VisualElement _root;

        [SetUp]
        public void SetUp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MenuPath);
            Assert.That(asset, Is.Not.Null, MenuPath);
            _root = asset.Instantiate();
        }

        [TestCase("profile-button")]
        [TestCase("play-with-bots")]
        [TestCase("online-button")]
        public void MainColumn_HoldsOnlyTheEverydayActions(string name)
        {
            VisualElement column = _root.Q<VisualElement>("columns");

            Assert.That(column.Q<VisualElement>(name), Is.Not.Null, name);
        }

        [TestCase("nickname-input")]
        [TestCase("random-nickname")]
        [TestCase("skins")]
        public void ProfileControls_LiveInTheCustomizeWindow(string name)
        {
            Assert.That(_root.Q<VisualElement>("columns").Q<VisualElement>(name), Is.Null, name);
            Assert.That(_root.Q<VisualElement>("customize-overlay").Q<VisualElement>(name), Is.Not.Null, name);
        }

        [TestCase("quick-play")]
        [TestCase("create-room")]
        [TestCase("join-room")]
        [TestCase("room-code-input")]
        public void OnlineControls_LiveInTheOnlineWindow(string name)
        {
            Assert.That(_root.Q<VisualElement>("columns").Q<VisualElement>(name), Is.Null, name);
            Assert.That(_root.Q<VisualElement>("online-overlay").Q<VisualElement>(name), Is.Not.Null, name);
        }

        [TestCase("customize-close")]
        [TestCase("online-close")]
        [TestCase("customize-status")]
        [TestCase("online-status")]
        public void Windows_CanBeClosedAndShowStatus(string name)
        {
            Assert.That(_root.Q<VisualElement>(name), Is.Not.Null, name);
        }

		[Test]
		public void MainColumn_HasThreeDirectItems()
		{
			Assert.That(_root.Q<VisualElement>("columns").childCount, Is.EqualTo(3));
		}

		[Test]
		public void MainMenu_ShowsVersionButKeepsProgressionRankOffScreen()
		{
			Assert.That(_root.Q<VisualElement>("version-text"), Is.Not.Null);
			Assert.That(_root.Q<VisualElement>("rank-dust"), Is.Null);
			Assert.That(_root.Q<VisualElement>("menu-rank-progress-fill"), Is.Null);
		}
    }
}
