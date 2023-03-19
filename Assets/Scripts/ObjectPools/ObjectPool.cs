using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Planet_IO.ObjectPool
{
    public abstract class ObjectPool<T> : MonoBehaviour, IInit where T : MonoBehaviour
    {
        [field: SerializeField] public int Count { get; set; } = 100;
        [field: SerializeField] public List<T> Prefabs { get; set; }
        [field: SerializeField] public IObjectPool<T> Pool { get; set; }
        public virtual void Init(){}
        
        protected virtual T OnCreate()
        {
            var randomNumber = Random.Range(0, Prefabs.Count);
            var go = Instantiate(Prefabs[randomNumber]);
            go.gameObject.SetActive(false);
            return go;
        }

        protected virtual void OnGet(T obj)
        {
            obj.gameObject.SetActive(true);
            obj.transform.SetParent(transform, true);
        }

        protected virtual void OnRelease(T @object) => @object.gameObject.SetActive(false);

        protected virtual void Destroy(T @object) => Destroy(@object.gameObject);
    }
}