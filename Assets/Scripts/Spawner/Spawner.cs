using System.Diagnostics;
using Planet_IO.ObjectPool;
using UnityEngine;

namespace Planet_IO
{
    public abstract class Spawner<T> : MonoBehaviour, IInit<T>, ICreate
        where T : MonoBehaviour, ICapacity
    {
        [SerializeField] private float _minObjectScale = 0.4f;
        [SerializeField] private float _maxObjectScale = 1f;
        [field: SerializeField] public Vector2 SpawnPositionX { get; set; } = new(-223f, 223f);
        [field: SerializeField] public Vector2 SpawnPositionY { get; set; } = new(-139f, 161.9f);

        protected ObjectPool<T> _objectPool;

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
        }
        protected virtual void GenerateObjects()
        {
#if UNITY_EDITOR
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
#endif
            for (int i = 0; i < _objectPool.Count; i++)
            {
                CreateObject();
            }

#if UNITY_EDITOR
            stopwatch.Stop();
            print(stopwatch.ElapsedMilliseconds + " ms");
#endif
        }
        protected virtual void SetTransform(T @object, float randomScale)
        {
            if (@object == null)
                return;

            var randomPosition = GetRandomPosition();
            var objectTransform = @object.transform;
            objectTransform.position = randomPosition;
            var zPosition = 1;
            objectTransform.localScale = new Vector3(randomScale, randomScale, zPosition);
        }
        protected virtual Vector2 GetRandomPosition() =>
            new(Random.Range(SpawnPositionX.x, SpawnPositionX.y),
                Random.Range(SpawnPositionY.x, SpawnPositionY.y));
    }
}