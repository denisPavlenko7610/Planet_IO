using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PlanetIO
{
    public sealed class BoostButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IBoostInput
    {
        private bool _pointerPressed;
        private bool _keyboardPressed;

        public event Action<bool> BoostChanged;

        public void OnPointerDown(PointerEventData eventData)
        {
            SetPointerPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPointerPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPointerPressed(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            bool keyboardPressed = keyboard != null && keyboard.spaceKey.isPressed;
            if (_keyboardPressed == keyboardPressed)
            {
                return;
            }

            _keyboardPressed = keyboardPressed;
            NotifyPressedState();
        }

        private void OnDisable()
        {
            _pointerPressed = false;
            _keyboardPressed = false;
            NotifyPressedState();
        }

        private void SetPointerPressed(bool isPressed)
        {
            _pointerPressed = isPressed;
            NotifyPressedState();
        }

        private void NotifyPressedState()
        {
            BoostChanged?.Invoke(_pointerPressed || _keyboardPressed);
        }
    }
}
