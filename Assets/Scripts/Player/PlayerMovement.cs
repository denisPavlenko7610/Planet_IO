using System;
using Dythervin.AutoAttach;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Script Player")]
        [SerializeField, Attach(Attach.Scene)] private AccelerationButton _accelerationButton;
        [SerializeField, Attach] private Player _player;
        [SerializeField, Attach(Attach.Child)] private Arrow _arrowPlayer;
        
        [Space]
        [SerializeField, Attach(Attach.Scene)] private Camera mainCamera;
        [SerializeField, Attach] private Rigidbody2D rigidbody2D;
        
        [Header("Speed")]
        [SerializeField] private float _normalSpeed = 3f;
        [SerializeField] private float _boostSpeed;
        [SerializeField] private float _timeToTick = 1f;
        private float _currentSpeed;
        private bool _isBoost;
        
        private float _angle;
        private Vector2 _mousePosition;
        private Coroutine _boostCoroutine;
        private PlayerInput _playerInput;
        private  Vector2 _direction;
        public Action<Vector2> Input;
        private bool _mouse = true;

        private void Awake() => _currentSpeed = _normalSpeed;

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
            _playerInput.Disable();
            if (_playerInput != null) _playerInput.SwitchDevice.Switch.performed -= SwitchDevice;
            if (_playerInput != null) _playerInput.Joystick.MoveJoystick.performed -= UpdateInputJoystick;
            if (_playerInput != null) _playerInput.Joystick.MoveJoystick.canceled -= CanceledInputJoystick;
            if (_playerInput != null) _playerInput.Mouse.MoveMouse.performed -= UpdateInputMouse;
        } 

        //Joystick 
        private void UpdateInputMouse(InputAction.CallbackContext ctx)
        {
            _direction = mainCamera.ScreenToWorldPoint(_playerInput.Mouse.MoveMouse.ReadValue<Vector2>()) - transform.position;
            Input?.Invoke(_direction);
        }

        private void UpdateInputJoystick(InputAction.CallbackContext ctx)
        {
            _direction = _playerInput.Joystick.MoveJoystick.ReadValue<Vector2>();
            Input?.Invoke(_direction);
        }

        private void CanceledInputJoystick(InputAction.CallbackContext ctx)
        {
            _direction = Vector2.zero;
            Input?.Invoke(_direction);
        }

        private void SwitchDevice(InputAction.CallbackContext ctx)
        {
            _mouse = !_mouse;
            if (!_mouse)
            {
                _playerInput.Mouse.Disable();
                _playerInput.Joystick.Enable();
            }
            else
            {
                _playerInput.Mouse.Enable();
                _playerInput.Joystick.Disable();
            }
          
        }
        void Update()
        {
            if (_accelerationButton.IsPressed)
            {
                Boost();
            }
            else
            {
                SetNormalSpeed();
            }
        }
        
        private void RotationPlayer()
        {
            _angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            rigidbody2D.rotation = _angle;
        }


        private void FixedUpdate()
        {
            MovePlayer(); 
            RotationPlayer();
        }
        private void Boost()
        {
            _isBoost = true;
            _currentSpeed = _boostSpeed;
            _boostCoroutine ??= StartCoroutine(ScaleBoost());
        }

        private void SetNormalSpeed()
        {
            _isBoost = false;
            _currentSpeed = _normalSpeed;
        }

        private void MovePlayer()
        {
            rigidbody2D.velocity = _direction.normalized * _currentSpeed;
        } 

        private IEnumerator ScaleBoost()
        {
            while (_isBoost)
            {
                _player.SpeedLogics();
                yield return new WaitForSeconds(_timeToTick);
            }
            _boostCoroutine = null;
        }
        
    }
}