using Cinemachine;
using UnityEngine;
using Zenject;

namespace PlanetIO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : MonoBehaviour
    {
        private CinemachineVirtualCamera _playerCamera;
        private PointsPool _pointsPool;
        private PointsGenerator _pointsGenerator;

        [Inject]
        private void Construct(CinemachineVirtualCamera playerCamera, PointsPool pointsPool, PointsGenerator pointsGenerator)
        {
            _playerCamera = playerCamera;
            _pointsPool = pointsPool;
            _pointsGenerator = pointsGenerator;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out Point point))
            {
                IncreaseScale(point.Capacity);
                IncreaseFov(point.Capacity);
                _pointsPool.Pool.Release(point);
                _pointsGenerator.CreatePoint();
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