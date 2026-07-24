using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace PlanetIO.ObjectPool
{
    public abstract class ObjectPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _capacity = 100;
        [SerializeField] private List<T> _prefabs = new();

        private IObjectResolver _objectResolver;

        protected virtual int MinimumCapacity => 1;
        public int Capacity => Mathf.Max(_capacity, MinimumCapacity);
        private IObjectPool<T> _pool;

        protected virtual int MaximumPoolSize => Capacity;

        [Inject]
        public void Construct(IObjectResolver objectResolver)
        {
            _objectResolver = objectResolver
                ?? throw new ArgumentNullException(nameof(objectResolver));
        }

        public void Initialize()
        {
            _pool ??= new UnityEngine.Pool.ObjectPool<T>(
                CreatePooledObject,
                ActivatePooledObject,
                DeactivatePooledObject,
                DestroyPooledObject,
                false,
                Capacity,
                MaximumPoolSize);
        }

        public T Get()
        {
            if (_pool == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} must be initialized before use.");
            }

            return _pool.Get();
        }

        protected virtual T CreatePooledObject()
        {
            if (_prefabs == null || _prefabs.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} requires at least one prefab.");
            }

            int prefabIndex = Random.Range(0, _prefabs.Count);
            T prefab = _prefabs[prefabIndex];
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} contains a missing prefab reference.");
            }

            T pooledObject = _objectResolver.Instantiate(prefab);
            pooledObject.gameObject.SetActive(false);
            return pooledObject;
        }

        protected virtual void ActivatePooledObject(T pooledObject)
        {
            pooledObject.gameObject.SetActive(true);
        }

        protected virtual void DeactivatePooledObject(T pooledObject)
        {
            pooledObject.gameObject.SetActive(false);
        }

        protected virtual void DestroyPooledObject(T pooledObject)
        {
            Destroy(pooledObject.gameObject);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            _capacity = Mathf.Max(MinimumCapacity, _capacity);
        }
#endif
    }
}
