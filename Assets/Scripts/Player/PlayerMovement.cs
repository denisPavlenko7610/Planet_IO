using System;
using Dythervin.AutoAttach;
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;


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
        private float _offSet = 90f;
        private Vector2 _mousePosition;
        private Coroutine _boostCoroutine;
        private PlayerInput _playerInput;

        private void Awake()
        {
            _currentSpeed = _normalSpeed;
            _playerInput = new PlayerInput();
        }

        private void OnEnable() => _playerInput.Enable();

        private void OnDisable() => _playerInput.Disable();

        void Update()
        { 
            _mousePosition = mainCamera.ScreenToWorldPoint(_playerInput.Player.Move.ReadValue<Vector2>()) - transform.position;
       
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
            _angle = Mathf.Atan2(_mousePosition.y, _mousePosition.x) * Mathf.Rad2Deg;
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
        private void MovePlayer() => rigidbody2D.velocity = _mousePosition.normalized * _currentSpeed;

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