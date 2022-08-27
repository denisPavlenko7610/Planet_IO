using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Planet_IO
{
    public class AccelerationButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action<bool> IsPressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed?.Invoke(false);
        }
    }
}