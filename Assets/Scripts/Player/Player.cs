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
        private CinemachineVirtualCamera _playerCamera;
        private IPool<Point> _pointsObjectPool;
        private IPool<Comet> _cometsPool;
        private ISpawner<Point> _pointsSpawner;
        private CometsSpawner cometsSpawner;

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera, IPool<Point> pointsPool, IPool<Comet> cometsPool,
            ISpawner<Point> pointsSpawner)
        {
            _playerCamera = playerCamera;
            _pointsObjectPool = pointsPool;
            _cometsPool = cometsPool;
            _pointsSpawner = pointsSpawner;
            this.cometsSpawner = cometsSpawner;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                IncreaseScale(point.Capacity);
                IncreaseFov(point.Capacity);
                _pointsObjectPool.Pool.Release(point);
                _pointsSpawner.CreateObject();
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _cometsPool.Pool.Release(comet);
                cometsSpawner.CreateComet();
            }
        }

        private void IncreaseScale(float scaleValue)
        {
            var scaleDivider = 100;
            scaleValue /= scaleDivider;
            transform.localScale += new Vector3(scaleValue, scaleValue, 0);
        }

        private void IncreaseFov(float scaleValue)
        {
            var scaleDivider = 40;
            scaleValue /= scaleDivider;
            _playerCamera.m_Lens.OrthographicSize += scaleValue;
        }
    }
}