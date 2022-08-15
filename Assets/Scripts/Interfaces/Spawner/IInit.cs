using Pool;
using UnityEngine;

namespace Spawner
{
    public interface IInit<T> where T: MonoBehaviour
    {
        void Init(IPool<T> pool);
    }
}