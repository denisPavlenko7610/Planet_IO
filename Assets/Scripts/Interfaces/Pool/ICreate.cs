using UnityEngine;

namespace Pool
{
    public interface ICreate<out T> where T : MonoBehaviour
    {
        T OnCreate();
    }
}