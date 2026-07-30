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
        private const float ScaleDepth = 1f;

        [SerializeField, Min(0.01f)] private float _minimumObjectScale = 0.1f;
        [SerializeField, Min(0.01f)] private float _maximumObjectScale = 1f;
        [SerializeField] private Vector2 _horizontalSpawnRange = new(-66f, 56f);
        [SerializeField] private Vector2 _verticalSpawnRange = new(-36f, 116f);

        private ObjectPool<T> _objectPool;

        protected float MinimumObjectScale => _minimumObjectScale;

        public void Initialize(ObjectPool<T> objectPool)
        {
            _objectPool = objectPool ?? throw new ArgumentNullException(nameof(objectPool));
            _objectPool.Initialize();
        }

        public T CreateObject()
        {
            float scale = GetRandomScale();
            return CreateSpawnedObject(GetRandomPosition(), scale);
        }

        public T CreateObject(Transform spawnTransform)
		{
			if (spawnTransform == null)
            {
                throw new ArgumentNullException(nameof(spawnTransform));
            }

			return CreateSpawnedObject(spawnTransform.position, _minimumObjectScale);
		}

		private T CreateSpawnedObject(Vector2 position, float capacity)
		{
			T spawnedObject = _objectPool.Get();
            SetState(spawnedObject, position, capacity);
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
            SetState(objectToRespawn, GetRandomPosition(), randomScale);
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

        private static void SetState(T spawnedObject, Vector2 position, float capacity)
        {
            spawnedObject.Capacity = capacity;

            Transform spawnedTransform = spawnedObject.transform;
            spawnedTransform.position = position;
            float actualCapacity = spawnedObject.Capacity;
            spawnedTransform.localScale = new Vector3(
                actualCapacity,
                actualCapacity,
                ScaleDepth);
        }

        private float GetRandomScale()
        {
            float minimumScale = Mathf.Min(_minimumObjectScale, _maximumObjectScale);
            float maximumScale = Mathf.Max(_minimumObjectScale, _maximumObjectScale);
            return Random.Range(minimumScale, maximumScale);
        }

        private static void SpawnNetworkObject(T spawnedObject)
        {
            if (spawnedObject.TryGetComponent(out NetworkObject networkObject) &&
                !networkObject.IsSpawned &&
                networkObject.NetworkManager is { IsListening: true, IsServer: true })
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
