using System;
using PlanetIO.Core.Attributes;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : NetworkBehaviour, IMove
    {
        [SerializeField, Assign] private Player _player;
        [SerializeField, Assign] private Rigidbody2D _rigidbody2D;
        [SerializeField, Min(0.05f)] private float _timeToTick = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _massSpeedPenalty = 0.35f;
        [SerializeField, Range(0.1f, 1f)] private float _minimumSpeedMultiplier = 0.55f;
        [SerializeField, Min(1f)] private float _turnSpeed = 540f;

        [field: Header("Speed")]
        [field: SerializeField] public float NormalSpeed { get; set; } = 4f;
        [field: SerializeField] public float BoostSpeed { get; set; } = 8f;

        public Player Player => _player;
        public Vector2 Direction { get; private set; } = Vector2.right;

        private Vector2 _desiredDirection = Vector2.right;
        private IBoostInput _boostInput;
        private float _currentSpeed;
        private bool _boostInputSubscribed;
        private bool _isBoosting;

        private void Awake()
        {
            _player ??= GetComponent<Player>();
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
        }

        [Inject]
        public void Construct(IBoostInput boostInput)
        {
            UnsubscribeBoostInput();
            _boostInput = boostInput;

            if (isActiveAndEnabled)
            {
                SubscribeBoostInput();
            }
        }

        private void OnEnable()
        {
            SubscribeBoostInput();
        }

        private void OnDisable()
        {
            _isBoosting = false;
            _currentSpeed = NormalSpeed;
            UnsubscribeBoostInput();
        }

        private void FixedUpdate()
        {
            if (IsOwner && _player != null && _rigidbody2D != null)
            {
                UpdateDirection();

                float targetSpeed = _isBoosting ? BoostSpeed : NormalSpeed;
                float sizeAboveMinimum = Mathf.Max(0f, _player.Capacity - _player.MinCapacity);
                float speedMultiplier = Mathf.Clamp(
                    1f - sizeAboveMinimum * _massSpeedPenalty,
                    _minimumSpeedMultiplier,
                    1f);
                _currentSpeed = targetSpeed * speedMultiplier;
                Move();
            }
        }

        public void Move()
        {
            if (Direction == default)
            {
                Direction = _player.transform.right;
            }

            Vector2 normalizedDirection = Direction.normalized;
            float rotationAngle = Mathf.Atan2(
                normalizedDirection.y,
                normalizedDirection.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
            _rigidbody2D.linearVelocity = normalizedDirection * _currentSpeed;
        }

        public void SetDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude > 0.0001f)
            {
                _desiredDirection = moveInput.normalized;
            }
        }

        private void UpdateDirection()
        {
            if (Vector2.Angle(Direction, _desiredDirection) < 0.1f)
            {
                Direction = _desiredDirection;
                return;
            }

            Direction = Vector3.RotateTowards(
                Direction,
                _desiredDirection,
                _turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime,
                0f).normalized;
        }

        private void SubscribeBoostInput()
        {
            if (_boostInput == null || _boostInputSubscribed)
            {
                return;
            }

            _boostInput.BoostChanged += OnBoostChanged;
            _boostInputSubscribed = true;
        }

        private void UnsubscribeBoostInput()
        {
            if (_boostInput == null || !_boostInputSubscribed)
            {
                return;
            }

            _boostInput.BoostChanged -= OnBoostChanged;
            _boostInputSubscribed = false;
        }

        private void OnBoostChanged(bool isBoosting)
        {
            if (!IsOwner)
            {
                return;
            }

            if (!isBoosting)
            {
                _isBoosting = false;
                _currentSpeed = NormalSpeed;
                return;
            }

            if (_isBoosting)
            {
                return;
            }

            _isBoosting = true;
            _ = ActivatePlayerBoostLogicAsync();
        }

        private async Awaitable ActivatePlayerBoostLogicAsync()
        {
            _currentSpeed = BoostSpeed;

            try
            {
                while (_isBoosting)
                {
                    if (!_player.CanBoost)
                    {
                        _isBoosting = false;
                        break;
                    }

                    _player.EnableBoost();
                    await Awaitable.WaitForSecondsAsync(_timeToTick, destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The player was destroyed while the boost loop was awaiting its next tick.
            }
        }
    }
}
