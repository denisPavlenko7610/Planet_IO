using UnityEngine;

namespace Pool
{
    public interface IDestroy<in T> where T : MonoBehaviour
    {
        void OnDestroy(T @object);
    }
}