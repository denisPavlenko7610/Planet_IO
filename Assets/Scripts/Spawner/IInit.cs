using Planet_IO.ObjectPool;
using UnityEngine;

namespace Planet_IO
{
    public interface IInit<T> where T: MonoBehaviour
    {
        void Init(ObjectPool<T> objectPool);
    }
}