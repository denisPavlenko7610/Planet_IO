using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Planet_IO.ObjectPool
{
    public abstract class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [field: SerializeField] public int Count { get; set; } = 100;
        [field: SerializeField] public List<T> Prefabs { get; set; }
        [field: SerializeField] public IObjectPool<T> Pool { get; set; }

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public virtual void Initialize()
        {
        }
        
        protected virtual T OnCreate()
        {
            int randomNumber = Random.Range(0, Prefabs.Count);
            T prefab = Prefabs[randomNumber];
            T go = _resolver != null
                ? _resolver.Instantiate(prefab)
                : Instantiate(prefab);
            go.gameObject.SetActive(false);
            return go;
        }

        protected virtual void OnGet(T obj)
        {
            obj.gameObject.SetActive(true);
            //obj.transform.SetParent(transform, true);
        }

        protected virtual void OnRelease(T @object) => @object.gameObject.SetActive(false);

        protected virtual void Destroy(T @object) => Destroy(@object.gameObject);
    }
}
