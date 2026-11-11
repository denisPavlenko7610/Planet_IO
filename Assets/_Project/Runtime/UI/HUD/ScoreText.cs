using UnityEngine;
using UnityEngine.UIElements;

namespace PlanetIO.UI.Hud
{
    public interface IScoreView
    {
        void ShowScore(int score);
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class ScoreText : MonoBehaviour, IScoreView
    {
        private Label _scoreText;

        private void OnEnable()
        {
            _scoreText = GetComponent<UIDocument>().rootVisualElement.Q<Label>("score-text");
        }

        public void ShowScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = score.ToString();
            }
        }
    }
}
