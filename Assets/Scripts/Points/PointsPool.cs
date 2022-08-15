using System.Collections.Generic;
using Pool;
using UnityEngine;
using UnityEngine.Pool;

namespace PlanetIO
{
    public class PointsPool : MonoBehaviour, IPool<Point>
    {
        [field: SerializeField] public int Count { get; set; }
        [field: SerializeField] public List<Point> Prefabs { get; set; }
        [field: SerializeField] public IObjectPool<Point> Pool { get; set; }
        
        public void Init() =>
            Pool = new ObjectPool<Point>(OnCreate, OnGet, OnRelease, OnDestroy, false,
                Count, Count + Count);

        public Point OnCreate()
        {
            var randomNumber = Random.Range(0, Prefabs.Count);
            var go = Instantiate(Prefabs[randomNumber]);
            go.gameObject.SetActive(false);
            return go;
        }

        public void OnGet(Point @object)
        {
            @object.gameObject.SetActive(true);
            @object.transform.SetParent(transform, true);
        }

        public void OnRelease(Point @object) => @object.gameObject.SetActive(false);

        public void OnDestroy(Point @object) => Destroy(@object.gameObject);
    }
}