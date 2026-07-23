using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Planet_IO
{
    public sealed class AccelerationButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBoostInput
    {
        public event Action<bool> BoostChanged;

        public void OnPointerDown(PointerEventData eventData)
        {
            BoostChanged?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            BoostChanged?.Invoke(false);
        }
    }
}
