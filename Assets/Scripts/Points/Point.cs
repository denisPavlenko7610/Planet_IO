using UnityEngine;

namespace PlanetIO
{
    public class Point : MonoBehaviour
    {
        [field:SerializeField, Range(1,100)] public int Capacity { get; private set; } = 1;
    }
}