using Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Planet_IO
{
    public class PlayerScale : MonoBehaviour
    {
        [Header("Capacity Player")]
        public float minCapacityPlayer;
        [SerializeField] private float _maxCapacityPlayer;
        public float CapacityPlayer { get; private set; }

        private UnityEvent<float> _changeCapacity = new();
        private RestartGame _restartGame;
        private CinemachineVirtualCamera _playerCamera;

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera, RestartGame restartGame)
        {
            _playerCamera = playerCamera;
            _restartGame = restartGame;
        }

        private void Awake() => CapacityPlayer = transform.localScale.x;

        private void Start()
        {
            _changeCapacity.AddListener((capacity) =>
            {
                if (capacity < minCapacityPlayer) 
                    _restartGame.Restart();
            });
        }
        public void IncreasePlayerCapacity(float scaleValue)
        {
            if (CapacityPlayer < _maxCapacityPlayer)
            {
                IncreaseScale(scaleValue);
                IncreaseFieldOfView(scaleValue);
            }
        }
        
        public void DecreasePlayerCapacity(float scaleValue)
        {
            var scaleDivider = 2f;
            scaleValue /= scaleDivider;
            if (scaleValue <= minCapacityPlayer)
                scaleValue = minCapacityPlayer;
            CapacityPlayer = scaleValue;
            transform.localScale = new(scaleValue, scaleValue, 0);
        }

        private void IncreaseScale(float scaleValue)
        {
            var scaleDivider = 100;
            scaleValue /= scaleDivider;
            SetPlayerCapacity(scaleValue);
            _changeCapacity.Invoke(CapacityPlayer);
            transform.localScale += new Vector3(scaleValue, scaleValue, 0);
        }
        private void IncreaseFieldOfView(float scaleValue)
        {
            var scaleDivider = 40;
            scaleValue /= scaleDivider;
            _playerCamera.m_Lens.OrthographicSize += scaleValue;
        }
        
        private void SetPlayerCapacity(float scaleValue) => CapacityPlayer += scaleValue;
    }
}