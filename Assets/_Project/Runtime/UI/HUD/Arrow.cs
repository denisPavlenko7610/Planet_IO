using PlanetIO.Core.Attributes;
using UnityEngine;
using UnityEngine.UI;

namespace PlanetIO.UI.Hud
{
    public interface IDirectionArrowView
    {
        float DistanceFromPlayer { get; }
        float PlayerVisualRadius { get; }
        float SpriteAngleOffset { get; }
        float MaximumScale { get; }

        void Hide();
        void Show(Vector3 screenPosition, float angle, float scale);
    }

    public sealed class Arrow : MonoBehaviour, IDirectionArrowView
    {
        [SerializeField, Assign] private Image _arrowImage;
        [SerializeField, Min(0f)] private float _distanceFromPlayer = 0.65f;
        [SerializeField, Min(0.1f)] private float _playerVisualRadius = 3.55f;
        [SerializeField] private float _spriteAngleOffset = 90f;
        [SerializeField, Range(0.5f, 2f)] private float _maximumScale = 1.4f;

        public float DistanceFromPlayer => _distanceFromPlayer;
        public float PlayerVisualRadius => _playerVisualRadius;
        public float SpriteAngleOffset => _spriteAngleOffset;
        public float MaximumScale => _maximumScale;

        private void Awake()
        {
            _arrowImage.raycastTarget = false;
            Hide();
        }

        public void Hide()
        {
            _arrowImage.enabled = false;
        }

        public void Show(Vector3 screenPosition, float angle, float scale)
        {
            if (screenPosition.z < 0f)
            {
                Hide();
                return;
            }

            _arrowImage.enabled = true;
            _arrowImage.rectTransform.position = screenPosition;
            _arrowImage.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle + _spriteAngleOffset);
            _arrowImage.rectTransform.localScale = Vector3.one * scale;
        }
    }
}
