using System;
using Dythervin.AutoAttach;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        public Vector2 Direction { private get; set; } = Vector2.one;

        [Header("Script Player")] [SerializeField, Attach(Attach.Scene)]
        private AccelerationButton _accelerationButton;

        [SerializeField, Attach] private Player _player;

        [Space] [SerializeField, Attach] private Rigidbody2D rigidbody2D;

        [Header("Speed")] [SerializeField] private float _normalSpeed = 3f;
        [SerializeField] private float _boostSpeed = 6f;
        [SerializeField] private float _timeToTick = 1f;

        private float _currentSpeed;

        private float _rotationAngle;
        private bool _isBoost;

        private void Awake() => _currentSpeed = _normalSpeed;

        private void OnEnable()
        {
            _accelerationButton.IsPressed += EnableBoost;
            _accelerationButton.IsPressed += DisableBoost;
        }

        private void OnDisable()
        {
            _accelerationButton.IsPressed -= EnableBoost;
            _accelerationButton.IsPressed -= DisableBoost;
        }

        private void RotationPlayer()
        {
            _rotationAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            rigidbody2D.rotation = _rotationAngle;
        }

        private void FixedUpdate()
        {
            MovePlayer();
            RotationPlayer();
        }

        private void EnableBoost(bool isBoost)
        {
            if (!isBoost)
                return;
            
            _isBoost = true;
            var result = ActivatePlayerBoostLogic();
        }

        private void DisableBoost(bool isBoost)
        {
            if (isBoost)
                return;
            
            _isBoost = false;
            _currentSpeed = _normalSpeed;
        }

        private void MovePlayer() => rigidbody2D.velocity = Direction.normalized * _currentSpeed;

        private async UniTaskVoid ActivatePlayerBoostLogic()
        {
            _currentSpeed = _boostSpeed;

            while (_isBoost)
            {
                _player.EnableBoost();
                await UniTask.Delay(TimeSpan.FromSeconds(_timeToTick), ignoreTimeScale: false)
                    .SuppressCancellationThrow();
            }
        }
    }
}