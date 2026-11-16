using System.Collections;
using System.Text;
using NUnit.Framework;
using PlanetIO.UI.Hud;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest, Timeout(60000)]
        public IEnumerator DefeatScreen_WorstCaseContentStaysInsideTheCard()
        {
            yield return StartMoonGame();
            SessionHudView hud = Object.FindFirstObjectByType<SessionHudView>();
            hud.SetButtonLabels("Menü", "Nochmal spielen", "Weiter (Werbung)");
            hud.ShowDefeat("Zeit ist um!", "Platz 12 von 16\nPunkte 123.456\nRekord 123.456");
            hud.ShowRunResult(
                "Zeit: 12:34   Gefressen: 123   Stufe: 4\nGefressen von Extremely Long Player Name 12",
                "Rang 30",
                0.2f,
                0.9f,
                true,
                "Neuer Rang!\n+1.234 EP   +123 Sternenstaub");
            hud.SetDoubleDust(true, "Sternenstaub ×2 (Werbung)");
            hud.SetContinueVisible(true);
            yield return null;
            yield return null;
            yield return null;

            VisualElement root = hud.GetComponent<UIDocument>().rootVisualElement;
            VisualElement card = root.Q("defeat-card");
            var report = new StringBuilder();
            report.AppendLine($"root={root.worldBound} card={card.worldBound}");
            foreach (string columnName in new[] { "defeat-results", "defeat-actions" })
            {
                VisualElement column = card.Q(columnName);
                report.AppendLine($"{columnName} {column.worldBound}");
                foreach (VisualElement child in column.Children())
                {
                    report.AppendLine($"  {child.name,-16} display={child.resolvedStyle.display} {child.worldBound}");
                }
            }

            System.IO.File.WriteAllText("Temp/DefeatLayout.txt", report.ToString());
            ScreenCapture.CaptureScreenshot("Temp/DefeatWorstCase.png");
            yield return null;
            yield return null;

            Assert.That(card.worldBound.yMin, Is.GreaterThanOrEqualTo(root.worldBound.yMin - 0.5f), report.ToString());
            Assert.That(card.worldBound.yMax, Is.LessThanOrEqualTo(root.worldBound.yMax + 0.5f), report.ToString());
            Assert.That(card.worldBound.xMin, Is.GreaterThanOrEqualTo(root.worldBound.xMin - 0.5f), report.ToString());
            Assert.That(card.worldBound.xMax, Is.LessThanOrEqualTo(root.worldBound.xMax + 0.5f), report.ToString());
            foreach (string columnName in new[] { "defeat-results", "defeat-actions" })
            {
                VisualElement column = card.Q(columnName);
                float previousBottom = column.worldBound.yMin;
                foreach (VisualElement child in column.Children())
                {
                    if (child.resolvedStyle.display == DisplayStyle.None)
                    {
                        continue;
                    }

                    Rect bound = child.worldBound;
                    Assert.That(bound.yMin, Is.GreaterThanOrEqualTo(previousBottom - 0.5f), $"{child.name} overlaps the previous element.\n{report}");
                    Assert.That(bound.yMax, Is.LessThanOrEqualTo(card.worldBound.yMax + 0.5f), $"{child.name} leaves the card.\n{report}");
                    Assert.That(bound.xMin, Is.GreaterThanOrEqualTo(card.worldBound.xMin - 0.5f), $"{child.name} leaves the card.\n{report}");
                    Assert.That(bound.xMax, Is.LessThanOrEqualTo(card.worldBound.xMax + 0.5f), $"{child.name} leaves the card.\n{report}");
                    previousBottom = bound.yMax;
                }
            }

            hud.HideDefeat();
        }
    }
}
