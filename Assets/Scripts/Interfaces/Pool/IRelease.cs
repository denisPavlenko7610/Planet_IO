using UnityEngine;

namespace Pool
{
    public interface IRelease<in T> where T : MonoBehaviour
    {
        void OnRelease(T @object);
    }
}