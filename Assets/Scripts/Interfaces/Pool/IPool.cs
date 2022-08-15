using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Pool
{
    public interface IPool<T> : IInit, ICreate<T>, IGet<T>, IRelease<T>, IDestroy<T> where T : MonoBehaviour
    {
        public int Count { get; set; }
        public List<T> Prefabs { get; set; }
        public IObjectPool<T> Pool { get; set; }
    }
}