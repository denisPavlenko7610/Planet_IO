using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyMovement : NetworkBehaviour, IMove
    {
        public enum MovementState : byte
        {
            Inactive,
            Roaming,
            Evading
        }

        private const float MinimumDirectionSquaredMagnitude = 0.0001f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _normalSpeed = 3f;
        [SerializeField, Min(0f)] private float _turnSpeed = 360f;

        [Header("Roaming")]
        [SerializeField, Min(0.1f)]
        private float _minimumTimeToChangeDirection = 10f;

        [SerializeField, Min(0.1f)]
        private float _maximumTimeToChangeDirection = 30f;

        [Header("Evasion")]
        [SerializeField, Min(0.05f)] private float _evasionDuration = 1.25f;

        [Header("References")]
        [SerializeField] private Transform _enemyTransform;
        [SerializeField] private Rigidbody2D _rigidbody2D;

        private IGameStateService _gameStateService;
        private float _stateTimeRemaining;

        public Vector2 Direction { get; private set; } = Vector2.right;
        public MovementState State { get; private set; } = MovementState.Inactive;

        private void Awake()
        {
            _enemyTransform ??= transform;
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
        }

        [Inject]
        public void Construct(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService
                ?? throw new ArgumentNullException(nameof(gameStateService));
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                EnterRoaming();
            }
        }

        public override void OnNetworkDespawn()
        {
            StopMovement();
            State = MovementState.Inactive;
            base.OnNetworkDespawn();
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || _rigidbody2D == null)
            {
                return;
            }

            if (_gameStateService?.IsGameplayActive != true)
            {
                StopMovement();
                return;
            }

            TickState(Time.fixedDeltaTime);
            Move();
            RotateTowardsDirection(Time.fixedDeltaTime);
        }

        public void Move()
        {
            _rigidbody2D.linearVelocity = Direction * _normalSpeed;
        }

        public void EvadeFrom(Vector2 threatPosition)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            Vector2 directionAwayFromThreat =
                (Vector2)_enemyTransform.position - threatPosition;

            Direction = directionAwayFromThreat.sqrMagnitude >
                        MinimumDirectionSquaredMagnitude
                ? directionAwayFromThreat.normalized
                : GetRandomDirection();

            State = MovementState.Evading;
            _stateTimeRemaining = _evasionDuration;
        }

        private void TickState(float deltaTime)
        {
            if (State == MovementState.Inactive)
            {
                EnterRoaming();
                return;
            }

            _stateTimeRemaining -= deltaTime;
            if (_stateTimeRemaining > 0f)
            {
                return;
            }

            EnterRoaming();
        }

        private void EnterRoaming()
        {
            State = MovementState.Roaming;
            Direction = GetRandomDirection();

            float minimum = Mathf.Max(0.1f, _minimumTimeToChangeDirection);
            float maximum = Mathf.Max(minimum, _maximumTimeToChangeDirection);
            _stateTimeRemaining = Random.Range(minimum, maximum);
        }

        private void RotateTowardsDirection(float deltaTime)
        {
            if (Direction.sqrMagnitude <= MinimumDirectionSquaredMagnitude)
            {
                return;
            }

            float targetAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            _enemyTransform.rotation = Quaternion.RotateTowards(
                _enemyTransform.rotation,
                targetRotation,
                _turnSpeed * deltaTime);
        }

        private void StopMovement()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private static Vector2 GetRandomDirection()
        {
            Vector2 direction = Random.insideUnitCircle;
            return direction.sqrMagnitude > MinimumDirectionSquaredMagnitude
                ? direction.normalized
                : Vector2.right;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maximumTimeToChangeDirection = Mathf.Max(
                _minimumTimeToChangeDirection,
                _maximumTimeToChangeDirection);
        }
#endif
    }
}
