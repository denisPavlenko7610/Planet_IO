using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace PlanetIO
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(UIDocument))]
    public sealed class BoostButton : MonoBehaviour, IBoostInput
    {
        private VisualElement _button;
        private bool _pointerPressed;
        private bool _keyboardPressed;
        private int _pointerId = -1;

        public event Action<bool> BoostChanged;

        private void OnEnable()
        {
            _button = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("boost-button");
            _button.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _button.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _button.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            _button.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        private void OnDisable()
        {
            _button.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _button.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _button.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _button.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            _pointerPressed = false;
            _keyboardPressed = false;
            _pointerId = -1;
            NotifyPressedState();
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

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            if (_pointerId >= 0)
            {
                return;
            }

            _pointerId = pointerEvent.pointerId;
            _button.CapturePointer(_pointerId);
            pointerEvent.StopPropagation();
            SetPointerPressed(true);
        }

        private void OnPointerUp(PointerUpEvent pointerEvent)
        {
            if (pointerEvent.pointerId != _pointerId)
            {
                return;
            }

            _button.ReleasePointer(_pointerId);
            Release();
        }

        private void OnPointerCancel(PointerCancelEvent pointerEvent)
        {
            if (pointerEvent.pointerId == _pointerId)
            {
                Release();
            }
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent pointerEvent)
        {
            if (pointerEvent.pointerId == _pointerId)
            {
                Release();
            }
        }

        private void Release()
        {
            _pointerId = -1;
            SetPointerPressed(false);
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
