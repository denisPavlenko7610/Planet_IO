using Cinemachine;
using TMPro;
using Zenject;
using UnityEngine;

namespace Planet_IO
{
    public class PlayerScale : PlanetScale
    {
        [Header("UI")] 
        [SerializeField] private TextMeshProUGUI _UIScaleText;
        
        private CinemachineVirtualCamera _playerCamera;
        public bool IsDie { get; private set; }

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera) => _playerCamera = playerCamera;

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
            var scaleDivider = 40;
            scaleValue /= scaleDivider;
            _playerCamera.m_Lens.OrthographicSize += scaleValue;
            UpdateUI();
        }
        private void UpdateUI() => _UIScaleText.text = Capacity.ToString("F3");
    }
}