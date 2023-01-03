using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using RDTools.AutoAttach;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour, IMove
    {
        public Vector2 Direction { get; private set; } = Vector2.one;

        [Header("Player")] 
        [SerializeField, Attach(Attach.Scene)] private AccelerationButton _accelerationButton;
        [SerializeField, Attach] private InputPlayerSystem _inputPlayerSystem;
        [field:SerializeField, Attach] public Player Player { get; private set; }
        
        [Space] 
        [SerializeField, Attach] private Rigidbody2D _rigidbody2D;
        [SerializeField] private float _timeToTick = 1f;

        [field: Header("Speed")] 
        [field: SerializeField] public float NormalSpeed { get; set; } = 4f;
        [field: SerializeField] public float BoostSpeed { get; set; }  = 8f;

        private float _currentSpeed;

        private float _rotationAngle;
        private bool _isBoost;

        private void Awake() => _currentSpeed = NormalSpeed;

        private void OnEnable()
        {
            _accelerationButton.IsPressed += EnableBoost;
            _accelerationButton.IsPressed += DisableBoost;
            _inputPlayerSystem.Input += SetDirection;
        }

        private void OnDisable()
        {
            _accelerationButton.IsPressed -= EnableBoost;
            _accelerationButton.IsPressed -= DisableBoost;
            _inputPlayerSystem.Input -= SetDirection;
        }

        private void RotationPlayer()
        {
            _rotationAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            _rigidbody2D.rotation = _rotationAngle;
        }

        private void Update()
        {
            RotationPlayer();
        }

        private void FixedUpdate()
        {
            Move();
        }

        public void Move()
        {
            if (Direction == default)
            {
                Direction = Player.transform.forward;
            }
            
            _rigidbody2D.velocity = Direction.normalized * _currentSpeed;
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
            _currentSpeed = NormalSpeed;
        }

        private void SetDirection(Vector2 moveInput)
        {
            if (moveInput == default)
            {
                return;
            }
            
            Direction = moveInput;                     
        }

        private async UniTaskVoid ActivatePlayerBoostLogic()
        {
            _currentSpeed = BoostSpeed;

            while (_isBoost)
            {
                Player.EnableBoost();
                await UniTask.Delay(TimeSpan.FromSeconds(_timeToTick), ignoreTimeScale: false)
                    .SuppressCancellationThrow();
            }
        }
    }
}