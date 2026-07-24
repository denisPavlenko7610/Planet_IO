using System;
using PlanetIO.ObjectPool;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    public abstract class Spawner<T> : MonoBehaviour where T : MonoBehaviour, ICapacity
	{
        private const float SpawnDepth = 1f;

        private const int SpawnBatchSize = 3;

        [SerializeField, Min(0.01f)] private float _minimumObjectScale = 0.4f;
        [SerializeField, Min(0.01f)] private float _maximumObjectScale = 1f;
        [SerializeField] private Vector2 _horizontalSpawnRange = new(-223f, 223f);
        [SerializeField] private Vector2 _verticalSpawnRange = new(-139f, 161.9f);

        private ObjectPool<T> _objectPool;

        public void Initialize(ObjectPool<T> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));

            _objectPool.Initialize();
            GenerateInitialObjects();
        }

        public async Awaitable InitializeAsync(ObjectPool<T> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));

            _objectPool.Initialize();
            await GenerateInitialObjectsAsync();
        }

        public T CreateObject()
        {
			return CreateSpawnedObject();
        }

        public T CreateObject(Transform spawnTransform)
		{
			if (spawnTransform == null)
            {
                throw new ArgumentNullException(nameof(spawnTransform));
            }

			T spawnedObject = CreateSpawnedObject();
			SetTransform(spawnedObject, spawnTransform.position, _minimumObjectScale);
			return spawnedObject;
		}

		private T CreateSpawnedObject()
		{
			T spawnedObject = _objectPool.Get();
			spawnedObject.Capacity = _minimumObjectScale;
			SpawnNetworkObject(spawnedObject);
			return spawnedObject;
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

		protected void ReleaseObject(T objectToRelease)
		{
			if (objectToRelease == null)
			{
				return;
			}

			if (objectToRelease.TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
			{
				networkObject.Despawn(false);
			}

			_objectPool.Release(objectToRelease);
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

        private async Awaitable GenerateInitialObjectsAsync()
        {
            for (int objectIndex = 0; objectIndex < _objectPool.Capacity; objectIndex++)
            {
                CreateObject();

                if (objectIndex % SpawnBatchSize == SpawnBatchSize - 1)
                {
                    await Awaitable.NextFrameAsync();
                }
            }
        }

        private void SetRandomTransform(T spawnedObject, float scale)
        {
            SetTransform(spawnedObject, GetRandomPosition(), scale);
        }

        private static void SetTransform(T spawnedObject, Vector2 position, float scale)
        {
            Transform spawnedTransform = spawnedObject.transform;
            spawnedTransform.position = position;
            spawnedTransform.localScale = new Vector3(scale, scale, SpawnDepth);
        }

        private float GetRandomScale()
        {
            float minimumScale = Mathf.Min(_minimumObjectScale, _maximumObjectScale);
            float maximumScale = Mathf.Max(_minimumObjectScale, _maximumObjectScale);
            return Random.Range(minimumScale, maximumScale);
        }

        private static void SpawnNetworkObject(T spawnedObject)
        {
            if (spawnedObject.TryGetComponent(out NetworkObject networkObject) && !networkObject.IsSpawned)
            {
                networkObject.Spawn();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maximumObjectScale = Mathf.Max(_minimumObjectScale, _maximumObjectScale);

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
