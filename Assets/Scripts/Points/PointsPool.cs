using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public class PointsPool : MonoBehaviour
    {
        [field: SerializeField] public int PointCount { get; private set; } = 2000;
        [field: SerializeField] public Point[] Points { get; private set; }
        [field: SerializeField] public IObjectPool<Point> Pool { get; private set; }

        private void Awake()
        {
            var maxPointsMultiplier = 2;
            Pool = new ObjectPool<Point>(OnCreatePoint, OnGetPoint, OnReleasePoint, OnDestroyPoint, true,
                PointCount, PointCount * maxPointsMultiplier);
        }

        private void OnDestroyPoint(Point point)
        {
            Destroy(point.gameObject);
        }

        private void OnGetPoint(Point point)
        {
            point.gameObject.SetActive(true);
            point.transform.SetParent(transform, true);
        }

        private void OnReleasePoint(Point point)
        {
            point.gameObject.SetActive(false);
        }

        private Point OnCreatePoint()
        {
            var randomNumber = Random.Range(0, Points.Length);
            return Instantiate(Points[randomNumber]);
        }
    }
}