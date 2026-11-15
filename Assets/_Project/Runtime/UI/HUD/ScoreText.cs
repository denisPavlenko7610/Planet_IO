using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Hud
{
    public interface IScoreView
    {
        void ShowScore(int score);
        void ShowTier(int tier, float progress);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScoreText : MonoBehaviour, IScoreView
    {
        private const string TierClassPrefix = "tier-";

        private Label _scoreText;
        private VisualElement _tierPanel;
        private Label _tierText;
        private VisualElement _tierFill;
        private int _shownTier;

        private void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _scoreText = root.Q<Label>("score-text");
            _tierPanel = root.Q<VisualElement>("tier-panel");
            _tierText = root.Q<Label>("tier-text");
            _tierFill = root.Q<VisualElement>("tier-fill");
            _shownTier = 0;
        }

        public void ShowScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }
        }

        public void ShowTier(int tier, float progress)
        {
            if (_tierPanel == null || _tierText == null || _tierFill == null)
            {
                return;
            }

            int clampedTier = Mathf.Clamp(tier, TierRules.MinTier, TierRules.MaxTier);
            if (clampedTier != _shownTier)
            {
                _tierPanel.RemoveFromClassList(TierClassPrefix + _shownTier);
                _tierPanel.AddToClassList(TierClassPrefix + clampedTier);
                _tierText.text = ToRoman(clampedTier);
                _shownTier = clampedTier;
            }

            _tierFill.style.width = Length.Percent(Mathf.Clamp01(progress) * 100f);
        }

        private static string ToRoman(int tier) => tier switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            _ => "IV"
        };
    }
}
