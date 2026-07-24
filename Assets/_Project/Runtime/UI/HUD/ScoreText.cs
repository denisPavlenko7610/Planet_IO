using TMPro;
using UnityEngine;

namespace PlanetIO.UI.Hud
{
    public interface IScoreView
    {
        void ShowScore(int score);
    }

    public sealed class ScoreText : MonoBehaviour, IScoreView
    {
        [SerializeField] private TextMeshProUGUI _scoreText;

        public void ShowScore(int score)
        {
            _scoreText.text = score.ToString();
        }
    }
}
