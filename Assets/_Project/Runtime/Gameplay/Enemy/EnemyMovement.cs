using System;
using PlanetIO.Utils;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(Enemy))]
    public sealed class EnemyMovement : NetworkBehaviour, IMove
    {
        public enum MovementState : byte
        {
            Inactive,
            Roaming,
            Foraging,
            Hunting,
            Evading
        }

        private const int NearbyColliderCapacity = 48;
        private const float MinimumThinkInterval = 0.05f;
        private const float MinimumStateTime = 0.2f;
        private const float MinimumRoamTime = 0.1f;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _normalSpeed = 3.2f;
        [SerializeField, Min(0f)] private float _turnSpeed = 420f;
        [SerializeField, Range(0f, 1f)] private float _massSpeedPenalty = 0.28f;
        [SerializeField, Range(0.1f, 1f)]
        private float _minimumSpeedMultiplier = 0.58f;
        [SerializeField, Min(1f)] private float _huntSpeedMultiplier = 1.12f;
        [SerializeField, Min(1f)] private float _evadeSpeedMultiplier = 1.3f;

        [Header("Awareness")]
        [SerializeField, Min(1f)] private float _awarenessRadius = 22f;
        [SerializeField, Min(0.05f)] private float _thinkInterval = 0.24f;
        [SerializeField, Min(1f)] private float _huntSizeRatio = 1.12f;
        [SerializeField, Min(1f)] private float _threatSizeRatio = 1.05f;
        [SerializeField, Min(0.1f)] private float _hazardDistance = 5f;

        [Header("Roaming")]
        [SerializeField, Min(0.1f)]
        private float _minimumTimeToChangeDirection = 2.5f;
        [SerializeField, Min(0.1f)]
        private float _maximumTimeToChangeDirection = 6f;

        [Header("Evasion")]
        [SerializeField, Min(0.05f)] private float _evasionDuration = 1.4f;

        [Header("References")]
        [SerializeField] private Transform _enemyTransform;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Enemy _enemy;

        private readonly Collider2D[] _nearbyColliders =
            new Collider2D[NearbyColliderCapacity];
        private IGameStateService _gameStateService;
        private Vector2 _desiredDirection = Vector2.right;
        private float _stateTimeRemaining;
        private float _thinkTimeRemaining;

        public Vector2 Direction { get; private set; } = Vector2.right;
        public MovementState State { get; private set; } =
            MovementState.Inactive;

        private void Awake()
        {
            _enemyTransform ??= transform;
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
            _enemy ??= GetComponent<Enemy>();
        }

        [Inject]
        public void Construct(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                EnterRoaming();
                _thinkTimeRemaining = Random.Range(0f, _thinkInterval);
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
            if (!IsSpawned ||
                !IsServer ||
                _rigidbody2D == null ||
                _enemy == null)
            {
                return;
            }

            if (_gameStateService?.IsGameplayActive != true)
            {
                StopMovement();
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            _stateTimeRemaining -= deltaTime;
            _thinkTimeRemaining -= deltaTime;

            if (_thinkTimeRemaining <= 0f)
            {
                Think();
                _thinkTimeRemaining = Mathf.Max(MinimumThinkInterval, _thinkInterval);
            }

            if (State == MovementState.Roaming && _stateTimeRemaining <= 0f)
            {
                EnterRoaming();
            }
            else if (State == MovementState.Evading && _stateTimeRemaining <= 0f)
            {
                Think();
            }

            UpdateDirection(deltaTime);
            Move();
            RotateTowardsDirection(deltaTime);
        }

        public void Move()
        {
            float speedMultiplier =
                EnemyDecisionRules.GetCapacitySpeedMultiplier(_enemy.Capacity, _enemy.MinimumCapacity, _massSpeedPenalty,
                    _minimumSpeedMultiplier);

            float stateMultiplier = State switch
            {
                MovementState.Hunting => _huntSpeedMultiplier,
                MovementState.Evading => _evadeSpeedMultiplier,
                _ => 1f
            };

            _rigidbody2D.linearVelocity =
                Direction * (_normalSpeed * speedMultiplier * stateMultiplier);
        }

        public void EvadeFrom(Vector2 threatPosition)
        {
            if (!IsSpawned || !IsServer)
            {
                return;
            }

            Vector2 directionAway = (Vector2)_enemyTransform.position - threatPosition;
            _desiredDirection = directionAway.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude
                    ? directionAway.normalized
                    : GetRandomDirection();

            State = MovementState.Evading;
            _stateTimeRemaining = _evasionDuration;
        }

        private void Think()
        {
            Vector2 position = _enemyTransform.position;
            int hitCount = Physics2D.OverlapCircle(position, _awarenessRadius, ContactFilter2D.noFilter, _nearbyColliders);

            Player nearestPlayer = null;
            Point nearestFood = null;
            Vector2 hazardPosition = default;
            float playerDistanceSquared = float.PositiveInfinity;
            float foodDistanceSquared = float.PositiveInfinity;
            float hazardDistanceSquared = float.PositiveInfinity;

            for (int index = 0; index < hitCount; index++)
            {
                Collider2D candidate = _nearbyColliders[index];
                if (candidate == null || candidate.transform == _enemyTransform)
                {
                    continue;
                }

                Vector2 candidatePosition = candidate.transform.position;
                float distanceSquared = (candidatePosition - position).sqrMagnitude;

                if (candidate.TryGetComponent(out Player player) &&
                    distanceSquared < playerDistanceSquared)
                {
                    nearestPlayer = player;
                    playerDistanceSquared = distanceSquared;
                }
                else if (candidate.TryGetComponent(out Point point) && distanceSquared < foodDistanceSquared)
                {
                    nearestFood = point;
                    foodDistanceSquared = distanceSquared;
                }
                else if (candidate.TryGetComponent(out Comet _) && distanceSquared < hazardDistanceSquared)
                {
                    hazardPosition = candidatePosition;
                    hazardDistanceSquared = distanceSquared;
                }
            }

            bool hasImmediateHazard = hazardDistanceSquared <= _hazardDistance * _hazardDistance;
            EnemyIntent intent = EnemyDecisionRules.ChooseIntent(_enemy.Capacity, nearestPlayer?.Capacity ?? 0f,
                nearestFood != null, hasImmediateHazard, _huntSizeRatio, _threatSizeRatio);

            switch (intent)
            {
                case EnemyIntent.Evade:
                    Vector2 dangerPosition = hasImmediateHazard
                        ? hazardPosition
                        : nearestPlayer.transform.position;
                    EvadeFrom(dangerPosition);
                    break;

                case EnemyIntent.Hunt:
                    SetTarget(nearestPlayer.transform.position, MovementState.Hunting);
                    break;

                case EnemyIntent.Forage:
                    SetTarget(nearestFood.transform.position, MovementState.Foraging);
                    break;

                default:
                    if (State != MovementState.Roaming || _stateTimeRemaining <= 0f)
                    {
                        EnterRoaming();
                    }
                    break;
            }
        }

        private void SetTarget(Vector2 targetPosition, MovementState state)
        {
            Vector2 targetDirection = targetPosition - (Vector2)_enemyTransform.position;
            if (targetDirection.sqrMagnitude <= Constants.MinimumDirectionSquaredMagnitude)
            {
                return;
            }

            _desiredDirection = targetDirection.normalized;
            State = state;
            _stateTimeRemaining = Mathf.Max(_thinkInterval * 2f, MinimumStateTime);
        }

        private void EnterRoaming()
        {
            State = MovementState.Roaming;
            _desiredDirection = GetRandomDirection();

            float minimum = Mathf.Max(MinimumRoamTime, _minimumTimeToChangeDirection);
            float maximum = Mathf.Max(minimum, _maximumTimeToChangeDirection);
            _stateTimeRemaining = Random.Range(minimum, maximum);
        }

        private void UpdateDirection(float deltaTime)
        {
            if (_desiredDirection.sqrMagnitude <= Constants.MinimumDirectionSquaredMagnitude)
            {
                return;
            }

            Direction = Vector3.RotateTowards(Direction, _desiredDirection,
				_turnSpeed * Mathf.Deg2Rad * deltaTime, 0f).normalized;
        }

        private void RotateTowardsDirection(float deltaTime)
        {
            if (Direction.sqrMagnitude <= Constants.MinimumDirectionSquaredMagnitude)
            {
                return;
            }

            Quaternion targetRotation = Constants.DirectionToRotation(Direction);
            _enemyTransform.rotation = Quaternion.RotateTowards(_enemyTransform.rotation, targetRotation, _turnSpeed * deltaTime);
        }

        private void StopMovement()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private static Vector2 GetRandomDirection() => Constants.GetRandomDirection();

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maximumTimeToChangeDirection = Mathf.Max(_minimumTimeToChangeDirection, _maximumTimeToChangeDirection);
            _thinkInterval = Mathf.Max(MinimumThinkInterval, _thinkInterval);
            _awarenessRadius = Mathf.Max(_hazardDistance, _awarenessRadius);
        }
#endif
    }
}
