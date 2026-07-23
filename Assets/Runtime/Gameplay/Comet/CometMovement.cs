using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CometMovement : NetworkBehaviour, IMove
    {
        private const float MinimumDirectionSquaredMagnitude = 0.0001f;

        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Transform _cometTransform;
        [SerializeField, Min(0f)] private float _minimumSpeed = 0.01f;
        [SerializeField, Min(0f)] private float _maximumSpeed = 0.03f;

        private IGameStateService _gameStateService;
        private Vector2 _direction = Vector2.right;
        private float _normalSpeed;

        private void Awake()
        {
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
            _cometTransform ??= transform;
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
                ResetMovement();
            }
        }

        public override void OnNetworkDespawn()
        {
            StopMovement();
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

            Move();
            RotateTowardsDirection();
        }

        public void Move()
        {
            _rigidbody2D.linearVelocity = _direction * _normalSpeed;
        }

        private void ResetMovement()
        {
            float minimumSpeed = Mathf.Min(_minimumSpeed, _maximumSpeed);
            float maximumSpeed = Mathf.Max(_minimumSpeed, _maximumSpeed);
            _normalSpeed = Random.Range(minimumSpeed, maximumSpeed);

            Vector2 randomDirection = Random.insideUnitCircle;
            _direction =
                randomDirection.sqrMagnitude > MinimumDirectionSquaredMagnitude
                    ? randomDirection.normalized
                    : Vector2.right;
        }

        private void RotateTowardsDirection()
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            _cometTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void StopMovement()
        {
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maximumSpeed = Mathf.Max(_minimumSpeed, _maximumSpeed);
        }
#endif
    }
}
