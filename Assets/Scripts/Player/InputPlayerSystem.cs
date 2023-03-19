using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Planet_IO
{
    public class InputPlayerSystem : NetworkBehaviour
    {
        public Action<Vector2> Input { get; set; }

        private bool _mouse = true;
        private Vector2 _direction;
        private PlayerInput _playerInput;

        private void OnEnable()
        {
            _playerInput = new PlayerInput();
            _playerInput.Enable();
            _playerInput.Move.Movement.performed += UpdateInput;
            _playerInput.Move.Movement.canceled += CanceledInput;
        }

        private void OnDisable()
        {
            _playerInput?.Disable();
            if (_playerInput != null) _playerInput.Move.Movement.performed -= UpdateInput;
            if (_playerInput != null) _playerInput.Move.Movement.canceled -= CanceledInput;
        }

        private void UpdateInput(InputAction.CallbackContext ctx)
        {
            _direction = _playerInput.Move.Movement.ReadValue<Vector2>();
            Input?.Invoke(_direction);
        }

        private void CanceledInput(InputAction.CallbackContext ctx)
        {
            _direction = Vector2.zero;
            Input?.Invoke(_direction);
        }
    }
}