using System;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Planet_IO
{
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class Player : PlayerScale
    {
        private BordersTrigger _bordersTrigger;

        [Header("Mass balance")]
        [SerializeField, Min(0.001f)] private float _foodGrowthMultiplier = 0.015f;
        [SerializeField, Min(0.001f)] private float _cometDamageMultiplier = 0.02f;
        [SerializeField, Min(0.001f)] private float _boostMassCost = 0.004f;
        [SerializeField, Range(0f, 0.5f)] private float _borderMassLoss = 0.08f;
        [SerializeField, Min(1f)] private float _eatEnemySizeRatio = 1.08f;

        [Header("Boost food")]
        [SerializeField] private Transform _pointSpawnTransform;

        private IRespawnService<Comet> _cometRespawnService;
        private IRespawnService<Point> _pointRespawnService;
        private IRespawnService<Enemy> _enemyRespawnService;
        private ISpawnService<Point> _pointSpawnService;
        private INetworkSessionService _networkSessionService;
        private bool _servicesReady;
        private bool _isDefeated;

        public bool CanBoost => Capacity > MinCapacity + _boostMassCost;

        [Inject]
        public void Construct(
            IRespawnService<Comet> cometRespawnService,
            IRespawnService<Point> pointRespawnService,
            IRespawnService<Enemy> enemyRespawnService,
            ISpawnService<Point> pointSpawnService,
            INetworkSessionService networkSessionService)
        {
            _cometRespawnService = cometRespawnService;
            _pointRespawnService = pointRespawnService;
            _enemyRespawnService = enemyRespawnService;
            _pointSpawnService = pointSpawnService;
            _networkSessionService = networkSessionService;
            _servicesReady = true;
        }

        private void OnEnable()
        {
            _bordersTrigger = FindAnyObjectByType<BordersTrigger>();
            if (_bordersTrigger != null)
            {
                _bordersTrigger.OnPlayerTriggeredHandler += OnBorderHit;
            }
        }

        private void OnDisable()
        {
            if (_bordersTrigger != null)
            {
                _bordersTrigger.OnPlayerTriggeredHandler -= OnBorderHit;
            }
        }

        public void EnableBoost()
        {
            if (!IsOwner)
			{
				return;
			}

            ApplyBoostRpc();
        }

        [Rpc(SendTo.Server)]
        private void ApplyBoostRpc()
        {
            if (!_servicesReady || Capacity <= MinCapacity + _boostMassCost)
            {
                return;
            }

            if (Shrink(_boostMassCost))
            {
                CreatePointBehindPlayer();
            }
        }

        private void CreatePointBehindPlayer()
        {
            if (_pointSpawnTransform != null)
            {
                _pointSpawnService.SpawnAt(_pointSpawnTransform);
            }
        }

        private void OnBorderHit(float capacity)
        {
            if (IsServer)
            {
                Shrink(Mathf.Max(_boostMassCost, capacity * _borderMassLoss));
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer || !_servicesReady || other == null || _isDefeated)
            {
                return;
            }

            if (other.TryGetComponent(out Point point))
            {
                Grow(point.Capacity * _foodGrowthMultiplier);
                _pointRespawnService.Respawn(point);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                Shrink(comet.Capacity * _cometDamageMultiplier);
                _cometRespawnService.Respawn(comet);
            }
            else if (other.TryGetComponent(out Enemy enemy))
            {
                if (Capacity >= enemy.Capacity * _eatEnemySizeRatio)
                {
                    _enemyRespawnService.Respawn(enemy);
                }
                else
                {
                    Defeat();
                }
            }
        }

        private void Defeat()
        {
            if (_isDefeated)
            {
                return;
            }

            _isDefeated = true;
            if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }

            TriggerDefeatRpc();
        }

        [Rpc(SendTo.Owner)]
        private void TriggerDefeatRpc()
        {
            _ = ShutdownSessionAsync();
        }

        private async Awaitable ShutdownSessionAsync()
        {
            if (_networkSessionService != null)
            {
                await _networkSessionService.ShutdownAndReturnToMenuAsync();
            }
        }
    }
}
