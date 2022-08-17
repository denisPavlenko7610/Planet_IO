using System;
using Pool;
using Spawner;
using UnityEngine;
using Zenject;

namespace PlanetIO
{
    public class Borders : MonoBehaviour
    {
        private ObjectPool<Comet> _cometsObjectPool;
        private Spawner<Comet> _cometSpawner;

        [Inject]
        private void Construct(Spawner<Comet> cometSpawner, ObjectPool<Comet> cometsObjectPool)
        {
            _cometSpawner = cometSpawner;
            _cometsObjectPool = cometsObjectPool;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out Comet comet))
            {
                _cometsObjectPool.Pool.Release(comet);
                _cometSpawner.CreateObject();
            }
        }
    }
    
}

