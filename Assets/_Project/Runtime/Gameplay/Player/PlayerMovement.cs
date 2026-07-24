using System;
using PlanetIO.Core.Attributes;
using PlanetIO.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : NetworkBehaviour, IMove
    {
        [SerializeField, Assign] private Player _player;
        [SerializeField, Assign] private Rigidbody2D _rigidbody2D;
        [SerializeField, Min(0.05f)]
        private float _boostMassConsumptionInterval = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _massSpeedPenalty = 0.35f;
        [SerializeField, Range(0.1f, 1f)] private float _minimumSpeedMultiplier = 0.55f;
        [SerializeField, Min(1f)] private float _turnSpeed = 540f;

        [Header("Speed")]
        [SerializeField, Min(0f)] private float _normalSpeed = 4f;
        [SerializeField, Min(0f)] private float _boostSpeed = 8f;

        public Player Player => _player;
        public Vector2 Direction { get; private set; } = Vector2.right;

        private Vector2 _desiredDirection = Vector2.right;
        private IBoostInput _boostInput;
        private IGameStateService _gameStateService;
        private float _currentSpeed;
        private int _boostGeneration;
        private bool _boostInputSubscribed;
        private bool _isBoosting;

        private void Awake()
        {
            _player ??= GetComponent<Player>();
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
        }

        [Inject]
        public void Construct(IBoostInput boostInput, IGameStateService gameStateService)
        {
            UnsubscribeBoostInput();
            _boostInput = boostInput ?? throw new ArgumentNullException(nameof(boostInput));
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));

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
            _boostGeneration++;
            _currentSpeed = _normalSpeed;
            UnsubscribeBoostInput();
        }

        private void FixedUpdate()
        {
            if (!IsOwner || _player == null || _rigidbody2D == null)
            {
                return;
            }

            if (_gameStateService?.IsGameplayActive != true)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            UpdateDirection();

            float targetSpeed = _isBoosting ? _boostSpeed : _normalSpeed;
            float speedMultiplier = EnemyDecisionRules.GetCapacitySpeedMultiplier(_player.Capacity, _player.MinimumCapacity,
                _massSpeedPenalty, _minimumSpeedMultiplier);

            _currentSpeed = targetSpeed * speedMultiplier;
            Move();
        }

        public void Move()
        {
            if (Direction == default)
            {
                Direction = _player.transform.right;
            }

            Vector2 normalizedDirection = Direction.normalized;
            Quaternion targetRotation = Constants.DirectionToRotation(normalizedDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRotation, _turnSpeed * Time.fixedDeltaTime);
            _rigidbody2D.linearVelocity = normalizedDirection * _currentSpeed;
        }

        public void SetDirection(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude)
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

            Direction = Vector3.RotateTowards(Direction, _desiredDirection,
				_turnSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
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
            if (!IsOwner || _gameStateService?.IsGameplayActive != true)
            {
                return;
            }

            if (!isBoosting)
            {
                _isBoosting = false;
                _boostGeneration++;
                _currentSpeed = _normalSpeed;
                return;
            }

            if (_isBoosting)
            {
                return;
            }

            _isBoosting = true;
            int generation = ++_boostGeneration;
            _ = ActivatePlayerBoostLogicAsync(generation);
        }

        private async Awaitable ActivatePlayerBoostLogicAsync(
            int generation)
        {
            _currentSpeed = _boostSpeed;

            try
            {
                while (_isBoosting && generation == _boostGeneration)
                {
                    if (!_player.CanBoost)
                    {
                        _isBoosting = false;
                        break;
                    }

                    _player.EnableBoost();
                    await Awaitable.WaitForSecondsAsync(
                        _boostMassConsumptionInterval,
                        destroyCancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
				LoggerIO.LogError("The player was destroyed while the boost loop was awaiting its next tick");
            }
        }
    }
}
