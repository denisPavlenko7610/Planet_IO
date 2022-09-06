using Cinemachine;
using Zenject;

namespace Planet_IO
{
    public class PlayerScale : PlanetScale
    {
        private RestartGame _restartGame;
        private CinemachineVirtualCamera _playerCamera;

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera, RestartGame restartGame)
        {
            _playerCamera = playerCamera;
            _restartGame = restartGame;
        }

        private void Start() => Init();
        
        protected override void DeathCheck(float capacity)
        {
            if (capacity < MinCapacity)
                _restartGame.Restart();
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