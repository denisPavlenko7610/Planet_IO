using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed partial class CometMovement : NetworkBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Transform _cometTransform;
        [SerializeField, Min(0f)] private float _minimumSpeed = CometRules.MinimumSpeed;
        [SerializeField, Min(0f)] private float _maximumSpeed = CometRules.MaximumSpeed;

        private IGameStateService _gameStateService;
        private Collider2D _collider;
        private Vector2 _direction = Vector2.right;
        private float _normalSpeed;
        private ICometFoodTrail _foodSpawner;
        private IPlayerRegistry _players;
        private float _foodTimer;
        private float _retryTimer;
        private bool _waiting;

        [Inject]
        public void ConstructFlight(ICometFoodTrail foodSpawner, IPlayerRegistry players)
        {
            _foodSpawner = foodSpawner;
            _players = players;
        }

        private void Awake()
        {
            _rigidbody2D ??= GetComponent<Rigidbody2D>();
            _cometTransform ??= transform;
            _collider = GetComponent<Collider2D>();
        }

        [Inject]
        public void Construct(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SceneContainers.Attach(transform, SceneContainers.Comets);
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

            if (_waiting)
            {
                _retryTimer -= Time.fixedDeltaTime;
                if (_retryTimer <= 0f)
                {
                    Randomize();
                }
                return;
            }

            Move();
            _foodTimer -= Time.fixedDeltaTime;
            if (_foodTimer <= 0f)
            {
                _foodTimer = CometRules.TrailInterval;
                float radius = _collider.bounds.extents.magnitude;
                _foodSpawner?.TryLeaveCometFood(CometRules.FoodPosition(transform.position, _direction, radius));
            }
        }

        public void Randomize()
        {
            if (!IsServer)
            {
                return;
            }

            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector2 position = WorldBounds.RandomPosition(4f);
                if (_players?.IsAnyPlayerWithinDistance(position, CometRules.SpawnClearance) == true)
                {
                    continue;
                }

                Vector2 target = WorldBounds.RandomPosition(20f);
                Launch(position, target - position, Random.Range(_minimumSpeed, _maximumSpeed));
                return;
            }

            Launch(
                new Vector2(WorldBounds.Area.xMin - 100f, WorldBounds.Area.yMin - 100f),
                Vector2.right,
                0f
            );
            _waiting = true;
            _retryTimer = 1f;
            StopMovement();
        }

        public void Launch(Vector2 position, Vector2 direction, float speed)
        {
            if (!IsServer)
            {
                return;
            }

            _direction = Constants.NormalizedOrRight(direction);
            _normalSpeed = Mathf.Max(0f, speed);
            _waiting = false;
            _foodTimer = Random.Range(0.5f, CometRules.TrailInterval);
            _cometTransform.rotation = Constants.DirectionToRotation(_direction);
            transform.position = position;

            if (IsSpawned && TryGetComponent(out Unity.Netcode.Components.NetworkTransform networkTransform))
            {
                networkTransform.Teleport(position, _cometTransform.rotation, transform.localScale);
            }

            if (IsSpawned)
			{
				ClearTailRpc();
			}
			StopMovement();
        }

        [Rpc(SendTo.Everyone)]
        private void ClearTailRpc() => GetComponent<CometTailView>()?.Clear();

        public void Move()
        {
            _rigidbody2D.linearVelocity = _direction * _normalSpeed;
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