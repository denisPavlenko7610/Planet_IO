using System;
using System.Collections.Generic;
using System.Diagnostics;
using Planet_IO.ObjectPool;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    public abstract class Spawner<T> : MonoBehaviour, IInit<T>, ICreate
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
        private float zPosition = 1f;

        public virtual void Init(ObjectPool<T> objectPool)
        {
            _objectPool = objectPool;
            objectPool.Init();
            GenerateObjects();
        }

        public virtual void CreateObject()
        {
            var @object = _objectPool.Pool?.Get();
            var randomScale = Random.Range(_minObjectScale, _maxObjectScale);
            if (@object != null)
            {
                @object.Capacity = randomScale;
                SetTransform(@object, randomScale);
            }

            OnObjectCreated?.Invoke(@object);
            
            AddEnemyObjectToList(@object);
        }

        private void AddEnemyObjectToList(T @object)
        {
            if (@object is Enemy)
            {
                _createdObjects.Add(@object);
            }
        }

        public void CreateObject(Transform pos)
        {
            var @object = _objectPool.Pool?.Get();
            if (@object != null)
            {
                @object.Capacity = _minObjectScale;
                SetTransform(@object, pos);
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

        protected virtual void SetTransform(T @object, float randomScale)
        {
            if (@object == null)
                return;

            var randomPosition = GetRandomPosition();
            var objectTransform = @object.transform;
            objectTransform.position = randomPosition;
            objectTransform.localScale = new Vector3(randomScale, randomScale, zPosition);
        }

        private void SetTransform(T @object, Transform pos)
        {
            var objectTransform = @object.transform;
            objectTransform.position = pos.position;
            objectTransform.localScale = new Vector3(_minObjectScale, _minObjectScale, zPosition);
        }

        protected virtual Vector2 GetRandomPosition() =>
            new(Random.Range(SpawnPositionX.x, SpawnPositionX.y),
                Random.Range(SpawnPositionY.x, SpawnPositionY.y));
    }
}