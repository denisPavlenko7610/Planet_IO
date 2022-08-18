using System;
using Cinemachine;
using UnityEngine;
using Zenject;

namespace PlanetIO
{
    public class PlayerScale : MonoBehaviour
    {
        [Header("Capacity Player")]
        [SerializeField] private float _minCapacityPlayer;
        [SerializeField] private float _maxCapacityPlayer;
        public float _capacityPlayer { get; private set; }
        
        private CinemachineVirtualCamera _playerCamera;


        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera)
        {
            _playerCamera = playerCamera;
        }

        private void Awake()
        {
            _capacityPlayer = transform.localScale.x;
        }

        public void NewScalePlayer(float scaleValue)
        {
            IncreaseScale(scaleValue);
            IncreaseFov(scaleValue);
        }

        public void BordersInteraction(float scaleValue) => InteractionOnBorder(scaleValue);


        private void IncreaseScale(float scaleValue)
        {
            var scaleDivider = 100;
            scaleValue /= scaleDivider;
            NewCapacityPlayer(scaleValue);
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