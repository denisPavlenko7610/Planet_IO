using System.Collections.Generic;
using Pool;
using UnityEngine;
using UnityEngine.Pool;

namespace PlanetIO
{
    public class CometsPool : MonoBehaviour, IPool<Comet>
    {
        [field: SerializeField] public int Count { get; set; } = 100;
        [field: SerializeField] public List<Comet> Prefabs { get; set; }
        [field: SerializeField] public IObjectPool<Comet> Pool { get; set; }

        public void Init() =>
            Pool = new ObjectPool<Comet>(OnCreate, OnGet, OnRelease, OnDestroy, false,
                Count, Count);

        public Comet OnCreate()
        {
            var randomNumber = Random.Range(0, Prefabs.Count);
            var go = Instantiate(Prefabs[randomNumber]);
            go.gameObject.SetActive(false);
            return go;
        }

        public void OnGet(Comet @object)
        {
            @object.gameObject.SetActive(true);
            @object.transform.SetParent(transform, true);
        }

        public void OnRelease(Comet @object) => @object.gameObject.SetActive(false);

        public void OnDestroy(Comet @object) => Destroy(@object.gameObject);
    }
}