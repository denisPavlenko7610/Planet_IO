using Planet_IO.Utils;
using TMPro;
using UnityEngine;

namespace Planet_IO.UI
{
    public sealed class ScoreText : MonoBehaviour
    {
        [field: SerializeField] public TextMeshProUGUI UIScoreText { get; private set; }

        private PlayerScale _player;

        private void Update()
        {
            if (_player == null || !_player.IsSpawned || !_player.IsOwner)
            {
                BindLocalPlayer();
            }
        }

        private void OnDestroy()
        {
            UnbindPlayer();
        }

        private void BindLocalPlayer()
        {
            UnbindPlayer();

            foreach (Player candidate in FindObjectsByType<Player>(
                         FindObjectsInactive.Exclude))
            {
                if (!candidate.IsSpawned || !candidate.IsOwner)
                {
                    continue;
                }

                _player = candidate;
                _player.CapacityChanged += UpdateScore;
                UpdateScore(_player.Capacity);
                return;
            }
        }

        private void UnbindPlayer()
        {
            if (_player != null)
            {
                _player.CapacityChanged -= UpdateScore;
                _player = null;
            }
        }

        private void UpdateScore(float capacity)
        {
            if (UIScoreText != null)
            {
                UIScoreText.text = Mathf.RoundToInt(capacity * Constants.ScaleMultiplier)
                    .ToString();
            }
        }
    }
}
