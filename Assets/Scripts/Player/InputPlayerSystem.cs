using System;
using Dythervin.AutoAttach;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet_IO
{
    public class InputPlayerSystem : MonoBehaviour
    {
        public Action<Vector2> Input;
        [SerializeField, Attach(Attach.Scene)] private Camera mainCamera;
        [SerializeField, Attach(Attach.Child)] private Arrow _arrow;
        
        private bool _mouse = true;
        private Vector2 _direction;
        private PlayerInput _playerInput;


        private void OnEnable()
        {
            _playerInput = new PlayerInput();
            _playerInput.Enable();
            _playerInput.SwitchDevice.Switch.performed += SwitchDevice;
            _playerInput.Mouse.MoveMouse.performed += UpdateInputMouse;
            _playerInput.Joystick.MoveJoystick.performed += UpdateInputJoystick;
            _playerInput.Joystick.MoveJoystick.canceled += CanceledInputJoystick;
        }

        private void OnDisable()
        {
            _playerInput?.Disable();
            if (_playerInput != null) _playerInput.SwitchDevice.Switch.performed -= SwitchDevice;
            if (_playerInput != null) _playerInput.Mouse.MoveMouse.performed -= UpdateInputMouse;
            if (_playerInput != null) _playerInput.Joystick.MoveJoystick.performed -= UpdateInputJoystick;
            if (_playerInput != null) _playerInput.Joystick.MoveJoystick.canceled -= CanceledInputJoystick;
        }

        private void UpdateInputMouse(InputAction.CallbackContext ctx)
        {
            _direction = mainCamera.ScreenToWorldPoint(_playerInput.Mouse.MoveMouse.ReadValue<Vector2>()) - transform.position;
            Input?.Invoke(_direction);
        }
        private void UpdateInputJoystick(InputAction.CallbackContext ctx)
        {
            _direction = _playerInput.Joystick.MoveJoystick.ReadValue<Vector2>();
            _arrow.gameObject.SetActive(true);
            Input?.Invoke(_direction);
        }
        private void CanceledInputJoystick(InputAction.CallbackContext ctx)
        {
            _direction = Vector2.zero;
            _arrow.gameObject.SetActive(false);
            Input?.Invoke(_direction);
        }
        private void SwitchDevice(InputAction.CallbackContext ctx)
        {
            _mouse = !_mouse;
            if (!_mouse)
            {
                _playerInput.Mouse.Disable();
                _arrow.gameObject.SetActive(false);
                _playerInput.Joystick.Enable();
            }
            else
            {
                _playerInput.Mouse.Enable();
                _arrow.gameObject.SetActive(true);
                _playerInput.Joystick.Disable();
            }
          
        }


    }
}
