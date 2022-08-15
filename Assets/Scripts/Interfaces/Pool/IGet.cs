using UnityEngine;

namespace Pool
{
    public interface IGet<in T> where T : MonoBehaviour
    {
        void OnGet(T @object);
    }
}