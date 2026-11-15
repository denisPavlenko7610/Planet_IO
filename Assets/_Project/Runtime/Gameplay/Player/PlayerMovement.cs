using System;
using RD.Attributes;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace PlanetIO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMovement : NetworkBehaviour
    {
        [SerializeField, Assign] private Player _player;
        [SerializeField, Assign] private Rigidbody2D _rigidbody2D;
        [SerializeField, Range(0f, 1f)] private float _massSpeedPenalty = 0.35f;
        [SerializeField, Range(0.1f, 1f)] private float _minimumSpeedMultiplier = 0.55f;
        [SerializeField, Min(1f)] private float _turnSpeed = 540f;
        [SerializeField, Range(0.01f, 0.5f)] private float _turnSmoothTime = 0.09f;

        [Header("Speed")]
        [SerializeField, Min(0f)] private float _normalSpeed = 4f;
        [SerializeField, Min(0f)] private float _boostSpeed = 8f;

        public Player Player => _player;
        public Vector2 Direction { get; private set; } = Vector2.right;

        private Vector2 _desiredDirection = Vector2.right;
        private IBoostInput _boostInput;
        private IGameStateService _gameStateService;
        private float _currentSpeed;
        private float _turnVelocity;
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
            _gameStateService.StateChanged -= OnGameStateChanged;
            _gameStateService.StateChanged += OnGameStateChanged;

            if (isActiveAndEnabled)
            {
                SubscribeBoostInput();
            }
        }

        private void OnGameStateChanged(GameState _, GameState newState)
        {
            if (newState != GameState.Playing)
            {
                _isBoosting = false;
            }
        }

        public override void OnDestroy()
        {
            if (_gameStateService != null)
            {
                _gameStateService.StateChanged -= OnGameStateChanged;
            }

            base.OnDestroy();
        }

        private void OnEnable()
        {
            SubscribeBoostInput();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _player.Defeated += ResetBoostInput;
            _player.Revived += ResetBoostInput;
        }

        public override void OnNetworkDespawn()
        {
            _player.Defeated -= ResetBoostInput;
            _player.Revived -= ResetBoostInput;
            ResetBoostInput();
            base.OnNetworkDespawn();
        }

        private void ResetBoostInput() => _isBoosting = false;

        private void OnDisable()
        {
            _isBoosting = false;
            UnsubscribeBoostInput();
        }

        private void FixedUpdate()
        {
            if (!IsOwner || _player == null || _rigidbody2D == null)
            {
                return;
            }

            if (_player.IsDefeated ||
                _gameStateService?.IsGameplayActive != true)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
                _turnVelocity = 0f;
                return;
            }

            UpdateDirection();

            float targetSpeed = _isBoosting && _player.IsBoosting && _player.CanBoost ? _boostSpeed : _normalSpeed;
            float speedMultiplier = EnemyDecisionRules.GetCapacitySpeedMultiplier(_player.Capacity, _player.MinCapacity,
                _massSpeedPenalty, _minimumSpeedMultiplier);

            _currentSpeed = targetSpeed * speedMultiplier * _player.SpeedMultiplier;
            Move();
        }

        public void Move()
        {
            float targetAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            _rigidbody2D.MoveRotation(Mathf.MoveTowardsAngle(_rigidbody2D.rotation, targetAngle, _turnSpeed * Time.fixedDeltaTime));
            _rigidbody2D.linearVelocity = WorldBounds.KeepInside(_rigidbody2D.position, Direction * _currentSpeed);
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
            float current = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(_desiredDirection.y, _desiredDirection.x) * Mathf.Rad2Deg;
            float next = Mathf.SmoothDampAngle(current, target, ref _turnVelocity, _turnSmoothTime, _turnSpeed,
                Time.fixedDeltaTime);
            float radians = next * Mathf.Deg2Rad;
            Direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
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
            if (!IsOwner || _player == null || _gameStateService?.IsGameplayActive != true)
            {
                return;
            }

            _isBoosting = isBoosting;
            _player.SetBoostRpc(isBoosting);
        }
    }
}
