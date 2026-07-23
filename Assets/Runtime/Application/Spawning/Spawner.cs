using System;
using System.Collections.Generic;
using Planet_IO.ObjectPool;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    public abstract class Spawner<T> : MonoBehaviour
        where T : MonoBehaviour, ICapacity
    {
        [SerializeField] private float _minObjectScale = 0.4f;
        [SerializeField] private float _maxObjectScale = 1f;
        [field: SerializeField] public Vector2 SpawnPositionX { get; private set; } = new(-223f, 223f);
        [field: SerializeField] public Vector2 SpawnPositionY { get; private set; } = new(-139f, 161.9f);

        private List<T> _createdObjects = new();
        public event Action<T> OnObjectCreated;
        public event Action<List<T>> OnObjectsInited;
        private ObjectPool<T> _objectPool;
        private const float ZPosition = 1f;

        public virtual void Initialize(ObjectPool<T> objectPool)
        {
            _objectPool = objectPool;
            objectPool.Initialize();
            GenerateObjects();
        }

        public virtual void CreateObject()
        {
            T obj = _objectPool.Pool?.Get();
            float randomScale = Random.Range(_minObjectScale, _maxObjectScale);
            if (obj != null)
            {
                obj.Capacity = randomScale;
                SetTransform(obj, randomScale);
                SpawnNetworkObject(obj);
            }

            if (obj != null)
            {
                OnObjectCreated?.Invoke(obj);
                _createdObjects.Add(obj);
            }
        }

        public void CreateObject(Transform pos)
        {
            T obj = _objectPool.Pool?.Get();
            if (obj == null)
			{
				return;
			}

			obj.Capacity = _minObjectScale;
            SetTransform(obj, pos);
            SpawnNetworkObject(obj);
            OnObjectCreated?.Invoke(obj);
            _createdObjects.Add(obj);
        }

        protected void RespawnObject(T obj)
        {
            if (obj == null)
            {
                return;
            }

            float randomScale = Random.Range(_minObjectScale, _maxObjectScale);
            obj.Capacity = randomScale;
            SetTransform(obj, randomScale);
            obj.gameObject.SetActive(true);

            if (obj.TryGetComponent(out NetworkObject networkObject) &&
                networkObject.IsSpawned &&
                obj.TryGetComponent(out NetworkTransform networkTransform))
            {
                networkTransform.Teleport(
                    obj.transform.position,
                    obj.transform.rotation,
                    obj.transform.localScale);
            }
        }

        protected virtual void GenerateObjects()
        {
// #if UNITY_EDITOR
//             Stopwatch stopwatch = new Stopwatch();
//             stopwatch.Start();
// #endif
            _createdObjects.Clear();
            for (int i = 0; i < _objectPool.Count; i++)
            {
                CreateObject();
            }
            
            OnObjectsInited?.Invoke(_createdObjects);

// #if UNITY_EDITOR
//             stopwatch.Stop();
//             print(stopwatch.ElapsedMilliseconds + " ms");
// #endif
        }

        protected virtual void SetTransform(T obj, float randomScale)
        {
            if (obj == null)
			{
				return;
			}

			Vector2 randomPosition = GetRandomPosition();
            Transform objectTransform = obj.transform;
            objectTransform.position = randomPosition;
            objectTransform.localScale = new Vector3(randomScale, randomScale, ZPosition);
        }

        private void SetTransform(T obj, Transform pos)
        {
            Transform objectTransform = obj.transform;
            objectTransform.position = pos.position;
            objectTransform.localScale = new Vector3(_minObjectScale, _minObjectScale, ZPosition);
        }

        protected virtual Vector2 GetRandomPosition() =>
            new(Random.Range(SpawnPositionX.x, SpawnPositionX.y),
                Random.Range(SpawnPositionY.x, SpawnPositionY.y));

        private static void SpawnNetworkObject(T obj)
        {
            if (obj.TryGetComponent(out NetworkObject networkObject) && !networkObject.IsSpawned)
            {
                networkObject.Spawn();
            }
        }
    }
}
