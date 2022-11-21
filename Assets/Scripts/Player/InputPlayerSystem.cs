using System;
using RDTools.AutoAttach;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet_IO
{
    public class InputPlayerSystem : MonoBehaviour
    {
        [SerializeField] private bool _useMouseInput;
        [SerializeField] private Camera _mainCamera;
        [SerializeField, Attach(Attach.Child)] private Arrow _arrow;
        public Action<Vector2> Input { get; set; }

        private bool _mouse = true;
        private Vector2 _direction;
        private PlayerInput _playerInput;

        private void OnValidate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = FindObjectOfType<Camera>();
            }
        }

        private void OnEnable()
        {
            _playerInput = new PlayerInput();
            _playerInput.Enable();
            if (_useMouseInput)
            {
                _playerInput.Move.Movement.performed += UpdateInputMouse;
            }
            else
            {
                _playerInput.Move.Movement.performed += UpdateInputJoystick;
                _playerInput.Move.Movement.canceled += CanceledInputJoystick;
            }
        }

        private void OnDisable()
        {
            _playerInput?.Disable();
            if (_playerInput != null) _playerInput.Move.Movement.performed -= UpdateInputMouse;
            if (_playerInput != null) _playerInput.Move.Movement.performed -= UpdateInputJoystick;
            if (_playerInput != null) _playerInput.Move.Movement.canceled -= CanceledInputJoystick;
        }

        private void UpdateInputMouse(InputAction.CallbackContext ctx)
        {
            _direction = _mainCamera.ScreenToWorldPoint(_playerInput.Move.Movement.ReadValue<Vector2>()) -
                         transform.position;
            Input?.Invoke(_direction);
        }

        private void UpdateInputJoystick(InputAction.CallbackContext ctx)
        {
            _direction = _playerInput.Move.Movement.ReadValue<Vector2>();
            _arrow.gameObject.SetActive(true);
            Input?.Invoke(_direction);
        }

        private void CanceledInputJoystick(InputAction.CallbackContext ctx)
        {
            _direction = Vector2.zero;
            _arrow.gameObject.SetActive(false);
            Input?.Invoke(_direction);
        }
    }
}