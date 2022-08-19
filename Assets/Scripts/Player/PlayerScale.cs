using Cinemachine;
using Core;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace PlanetIO
{
    public class PlayerScale : MonoBehaviour
    {
        private UnityEvent<float> _ChangeCapacity = new UnityEvent<float>();
        
        [Header("Capacity Player")]
        [SerializeField] private float _minCapacityPlayer;
        [SerializeField] private float _maxCapacityPlayer;
        public float _capacityPlayer { get; private set; }

        private RestartGame _restartGame;
        private CinemachineVirtualCamera _playerCamera;


        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera, RestartGame restartGame)
        {
            _playerCamera = playerCamera;
            _restartGame = restartGame;
        }

        private void Awake() => _capacityPlayer = transform.localScale.x;

        private void Start()
        {
            _ChangeCapacity.AddListener((capacity) =>
            {
                if (capacity < _minCapacityPlayer) 
                    _restartGame.Restart();
                
            });
        }
        public void NewScalePlayer(float scaleValue)
        {
            if (_capacityPlayer < _maxCapacityPlayer)
            {
                IncreaseScale(scaleValue);
                IncreaseFov(scaleValue);
            }
        }

        public void BordersInteraction(float scaleValue) => InteractionOnBorder(scaleValue);


        private void IncreaseScale(float scaleValue)
        {
            var scaleDivider = 100;
            scaleValue /= scaleDivider;
            NewCapacityPlayer(scaleValue);
            _ChangeCapacity.Invoke(_capacityPlayer);
            transform.localScale += new Vector3(scaleValue, scaleValue, 0);
        }

        private void IncreaseFov(float scaleValue)
        {
            var scaleDivider = 40;
            scaleValue /= scaleDivider;
            _playerCamera.m_Lens.OrthographicSize += scaleValue;
        }

        private void InteractionOnBorder(float scaleValue)
        {
            var scaleDivider = 2f;
            scaleValue /= scaleDivider;
            if (scaleValue <= _minCapacityPlayer)
                scaleValue = _minCapacityPlayer;
            _capacityPlayer = scaleValue;
            transform.localScale = new(scaleValue, scaleValue, 0);
        }

        private void NewCapacityPlayer(float scaleValue) => _capacityPlayer += scaleValue;
    }
}