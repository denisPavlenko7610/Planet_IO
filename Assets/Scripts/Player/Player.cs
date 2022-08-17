using System;
using Cinemachine;
using Pool;
using Spawner;
using UnityEngine;
using Zenject;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        [Header("Capacity Player")]
        [SerializeField] private float _minCapacityPlayer;
        [SerializeField] private float _maxCapacityPlayer;
        [SerializeField] private float _capacityPlayer;
        
        private CinemachineVirtualCamera _playerCamera;
        private ObjectPool<Point> pointsObjectObjectPool;
        private ObjectPool<Comet> cometsObjectPool;
        private Spawner<Point> _pointsSpawner;
        private Spawner<Comet> _cometSpawner;

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera, ObjectPool<Point> pointsObjectPool, ObjectPool<Comet> cometsObjectPool,
            Spawner<Point> pointsSpawner, Spawner<Comet> cometSpawner)
        {
            _playerCamera = playerCamera;
            pointsObjectObjectPool = pointsObjectPool;
            this.cometsObjectPool = cometsObjectPool;
            _pointsSpawner = pointsSpawner;
            _cometSpawner = cometSpawner;
        }

        private void Awake()
        {
            _capacityPlayer = transform.localScale.x;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                IncreaseScale(point.Capacity);
                IncreaseFov(point.Capacity);
                pointsObjectObjectPool.Pool.Release(point);
                _pointsSpawner.CreateObject();
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                IncreaseScale(-comet.Capacity);
                IncreaseFov(-comet.Capacity);
                cometsObjectPool.Pool.Release(comet);
                _cometSpawner.CreateObject();
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (col.collider.TryGetComponent(out BordersTrigger borders))
            {
                InteractionOnBorder(_capacityPlayer);
                IncreaseFov(-_capacityPlayer);
            }
        }

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