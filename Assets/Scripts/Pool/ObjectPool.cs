using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Pool
{
    public abstract class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [field: SerializeField] public int Count { get; private set; } = 100;
        [field: SerializeField] public List<T> Prefabs { get; private set; }
        [field: SerializeField] public IObjectPool<T> Pool { get; private set; }

        public void Awake()
        {
            var maxCount = Count + Count;
            Pool = new UnityEngine.Pool.ObjectPool<T>(OnCreate, OnGet, OnRelease, OnDestroy, false,
                    Count, maxCount);
        }

        protected virtual T OnCreate()
        {
            var randomNumber = Random.Range(0, Prefabs.Count);
            var go = Instantiate(Prefabs[randomNumber]);
            go.gameObject.SetActive(false);
            return go;
        }

        private void OnGet(T go)
        {
            go.gameObject.SetActive(true);
            go.transform.SetParent(transform, true);
        }
        
        private void OnRelease(T go) => go.gameObject.SetActive(false);
        private void OnDestroy(T go) => Destroy(go.gameObject);
    }
}