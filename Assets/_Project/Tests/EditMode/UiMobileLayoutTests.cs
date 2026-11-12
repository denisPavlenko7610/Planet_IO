using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace PlanetIO.Tests
{
    public sealed class UiMobileLayoutTests
    {
        private const string UiFolder = "Assets/_Project/UI/";

        [TestCase(".btn-cta")]
        [TestCase(".btn-long")]
        [TestCase(".btn-small")]
        [TestCase(".btn-hud")]
        public void ButtonSizeClasses_ShrinkLocalizedTextToFit(string selector)
        {
            string block = GetBlock("Common.uss", selector);
            StringAssert.Contains("-unity-text-auto-size: best-fit", block, selector);
        }

        [TestCase("Settings.uss", ".settings-panel")]
        [TestCase("Hud.uss", "#defeat-card")]
        public void ModalPanels_NeverExceedScreenWidth(string file, string selector)
        {
            string block = GetBlock(file, selector);
            StringAssert.Contains("max-width:", block, selector);
        }

        [TestCase("Common.uss", ".btn-hud", 100)]
        [TestCase("Common.uss", ".btn-small", 100)]
        [TestCase("Settings.uss", ".stepper-button", 80)]
        public void TouchTargets_AreAtLeastMinimumHeight(string file, string selector, int minimumHeight)
        {
            string block = GetBlock(file, selector);
            Match match = Regex.Match(block, @"height:\s*(\d+)px");
            Assert.That(match.Success, Is.True, selector);
            Assert.That(int.Parse(match.Groups[1].Value), Is.GreaterThanOrEqualTo(minimumHeight), selector);
        }

        [Test]
        public void PanelSettings_ExpandToFitMinimumLogicalSize()
        {
            string text = File.ReadAllText(UiFolder + "PanelSettings.asset");
            StringAssert.Contains("m_ScaleMode: 2", text);
            StringAssert.Contains("m_ScreenMatchMode: 2", text);
            StringAssert.Contains("m_ReferenceResolution: {x: 1280, y: 1080}", text);
        }

        [Test]
        public void Menu_HasPortraitLayout()
        {
            string block = GetBlock("Menu.uss", ".menu--portrait #columns");
            StringAssert.Contains("flex-direction: column", block);
        }

        private static string GetBlock(string file, string selector)
        {
            string text = File.ReadAllText(UiFolder + file);
            Match match = Regex.Match(text, Regex.Escape(selector) + @"\s*\{([^}]*)\}");
            Assert.That(match.Success, Is.True, $"{selector} not found in {file}");
            return match.Groups[1].Value;
        }
    }
}
