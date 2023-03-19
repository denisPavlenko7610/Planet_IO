using System;
using System.Threading.Tasks;
using UnityEngine;
using RDTools.AutoAttach;
using Unity.Netcode;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : NetworkBehaviour, IMove
    {
        public Vector2 Direction { get; private set; } = Vector2.one;
        
        [field:SerializeField] public Player Player { get; private set; }
        
        [Space] 
        [SerializeField, Attach] private Rigidbody2D _rigidbody2D;
        [SerializeField] private float _timeToTick = 1f;

        [field: Header("Speed")] 
        [field: SerializeField] public float NormalSpeed { get; set; } = 4f;
        [field: SerializeField] public float BoostSpeed { get; set; }  = 8f;

        private float _currentSpeed;
        private float _rotationAngle;
        private bool _isBoost;
        
        private AccelerationButton _accelerationButton;
        private InputPlayerSystem _inputPlayerSystem;
        private void Start()
        {
            _currentSpeed = NormalSpeed;
        }

        private void OnEnable()
        {
            _accelerationButton = FindObjectOfType<AccelerationButton>();
            _inputPlayerSystem = FindObjectOfType<InputPlayerSystem>();
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
            if (!IsOwner)
                return;
            
            RotationPlayer();
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
                return;
            
            Move();
        }

        public void Move()
        {
            if (Direction == default)
                Direction = Player.transform.forward;

            _rigidbody2D.velocity = Direction.normalized * _currentSpeed;
        }

        private void EnableBoost(bool isBoost)
        {
            if (!IsOwner || !isBoost)
                return;

            _isBoost = true;
            var result = ActivatePlayerBoostLogic();
        }

        private void DisableBoost(bool isBoost)
        {
            if (!IsOwner || isBoost)
                return;

            _isBoost = false;
            _currentSpeed = NormalSpeed;
        }

        private void SetDirection(Vector2 moveInput)
        {
            if (moveInput == default)
                return;

            Direction = moveInput;                     
        }

        private async Task ActivatePlayerBoostLogic()
        {
            _currentSpeed = BoostSpeed;

            while (_isBoost)
            {
                Player.EnableBoost();
                await Task.Delay(TimeSpan.FromSeconds(_timeToTick));
            }
        }
    }
}