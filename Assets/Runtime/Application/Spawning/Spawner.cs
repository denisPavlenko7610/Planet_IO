using System;
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
        private const float SpawnDepth = 1f;

        [SerializeField, Min(0.01f)] private float _minimumObjectScale = 0.4f;
        [SerializeField, Min(0.01f)] private float _maximumObjectScale = 1f;
        [SerializeField] private Vector2 _horizontalSpawnRange = new(-223f, 223f);
        [SerializeField] private Vector2 _verticalSpawnRange = new(-139f, 161.9f);

        private ObjectPool<T> _objectPool;

        public void Initialize(ObjectPool<T> objectPool)
        {
            _objectPool = objectPool
                ?? throw new ArgumentNullException(nameof(objectPool));

            _objectPool.Initialize();
            GenerateInitialObjects();
        }

        public void CreateObject()
        {
            T spawnedObject = _objectPool.Get();
            float randomScale = GetRandomScale();

            spawnedObject.Capacity = randomScale;
            SetRandomTransform(spawnedObject, randomScale);
            SpawnNetworkObject(spawnedObject);
        }

        public void CreateObject(Transform spawnTransform)
        {
            if (spawnTransform == null)
            {
                throw new ArgumentNullException(nameof(spawnTransform));
            }

            T spawnedObject = _objectPool.Get();
            spawnedObject.Capacity = _minimumObjectScale;
            SetTransform(spawnedObject, spawnTransform.position, _minimumObjectScale);
            SpawnNetworkObject(spawnedObject);
        }

        protected void RespawnObject(T objectToRespawn)
        {
            if (objectToRespawn == null)
            {
                return;
            }

            float randomScale = GetRandomScale();
            objectToRespawn.Capacity = randomScale;
            SetRandomTransform(objectToRespawn, randomScale);
            objectToRespawn.gameObject.SetActive(true);

            if (objectToRespawn.TryGetComponent(out NetworkObject networkObject) &&
                networkObject.IsSpawned &&
                objectToRespawn.TryGetComponent(out NetworkTransform networkTransform))
            {
                networkTransform.Teleport(
                    objectToRespawn.transform.position,
                    objectToRespawn.transform.rotation,
                    objectToRespawn.transform.localScale);
            }
        }

        protected virtual Vector2 GetRandomPosition()
        {
            return new Vector2(
                Random.Range(_horizontalSpawnRange.x, _horizontalSpawnRange.y),
                Random.Range(_verticalSpawnRange.x, _verticalSpawnRange.y));
        }

        private void GenerateInitialObjects()
        {
            for (int objectIndex = 0; objectIndex < _objectPool.Capacity; objectIndex++)
            {
                CreateObject();
            }
        }

        private void SetRandomTransform(T spawnedObject, float scale)
        {
            SetTransform(spawnedObject, GetRandomPosition(), scale);
        }

        private static void SetTransform(
            T spawnedObject,
            Vector2 position,
            float scale)
        {
            Transform spawnedTransform = spawnedObject.transform;
            spawnedTransform.position = position;
            spawnedTransform.localScale = new Vector3(scale, scale, SpawnDepth);
        }

        private float GetRandomScale()
        {
            float minimumScale = Mathf.Min(
                _minimumObjectScale,
                _maximumObjectScale);
            float maximumScale = Mathf.Max(
                _minimumObjectScale,
                _maximumObjectScale);
            return Random.Range(minimumScale, maximumScale);
        }

        private static void SpawnNetworkObject(T spawnedObject)
        {
            if (spawnedObject.TryGetComponent(out NetworkObject networkObject) &&
                !networkObject.IsSpawned)
            {
                networkObject.Spawn();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maximumObjectScale = Mathf.Max(
                _minimumObjectScale,
                _maximumObjectScale);

            NormalizeRange(ref _horizontalSpawnRange);
            NormalizeRange(ref _verticalSpawnRange);
        }

        private static void NormalizeRange(ref Vector2 range)
        {
            if (range.x > range.y)
            {
                (range.x, range.y) = (range.y, range.x);
            }
        }
#endif
    }
}
