using Cinemachine;
using Zenject;

namespace Planet_IO
{
    public class PlayerScale : PlanetScale
    {
        public bool IsDie { get; private set; }
        private CinemachineVirtualCamera _playerCamera;

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera) => _playerCamera = playerCamera;

        private void Start() => Init();
        
        protected override void DeathCheck(float capacity)
        {
            if (capacity < MinCapacity)
                IsDie = true;
        }

        public override void SetCapacity(float scaleValue)
        {
            base.SetCapacity(scaleValue);
            IncreaseFieldOfView(scaleValue);
        }

        private void IncreaseFieldOfView(float scaleValue)
        {
            var scaleDivider = 40;
            scaleValue /= scaleDivider;
            _playerCamera.m_Lens.OrthographicSize += scaleValue;
        }
    }
}