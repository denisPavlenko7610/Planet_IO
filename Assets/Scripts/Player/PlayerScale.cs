using Planet_IO.Utils;
using TMPro;
using Zenject;
using UnityEngine;

namespace Planet_IO
{
    public class PlayerScale : PlanetScale
    {
        [Header("UI")] 
        [SerializeField] private TextMeshProUGUI _UIScaleText;
        
        private UnityEngine.Camera _playerCamera;
        public bool IsDie { get; private set; }

        [Inject]
        private void Construct(UnityEngine.Camera playerCamera) => _playerCamera = playerCamera;

        private void Start()
        {
            Init();
            UpdateUI();
        }

        protected override void DeathCheck(float capacity)
        {
            if (capacity < MinCapacity)
                IsDie = true;
        }

        protected override void SetCapacity(float scaleValue)
        {
            base.SetCapacity(scaleValue);
            IncreaseFieldOfView(scaleValue);
            UpdateUI();
        }

        protected override void DecreaseCapacity(float scaleValue)
        {
            base.DecreaseCapacity(scaleValue);
            UpdateUI();
        }

        private void IncreaseFieldOfView(float scaleValue)
        {
            scaleValue /= Constants.ScaleDivider;
            _playerCamera.orthographicSize += scaleValue;
            UpdateUI();
        }
        private void UpdateUI() => _UIScaleText.text = (Capacity * Constants.ScaleMultiplier).ToString("F1");
    }
}