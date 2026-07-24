using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Planet_IO
{
    public sealed class AccelerationButton :
        MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        IBoostInput
    {
        private bool _isPressed;

        public event Action<bool> BoostChanged;

        public void OnPointerDown(PointerEventData eventData)
        {
            SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        private void SetPressed(bool isPressed)
        {
            if (_isPressed == isPressed)
            {
                return;
            }

            _isPressed = isPressed;
            BoostChanged?.Invoke(_isPressed);
        }
    }
}
