using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using Random = UnityEngine.Random;

namespace PlanetIO
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Rigidbody2D), typeof(Enemy))]
	public sealed partial class EnemyMovement : NetworkBehaviour
	{
		public enum MovementState : byte
		{
			Inactive,
			Roaming,
			Foraging,
			Hunting,
			Evading
		}

		private const int NearbyColliderCapacity = 24;
		private const float FoodSearchRadius = 16f;
		private const float FieldSeekChance = 0.7f;
		private const float FieldSeekMinimumDistance = 10f;
		private const float MinimumThinkInterval = 0.1f;
		private const float IdleCullDistance = 40f;
		private const float MinimumStateTime = 0.2f;
		private const float MinimumRoamTime = 0.1f;
		private const float SeparationRadiusPerUnit = 2f;
		private const float SeparationWeight = 0.8f;

		[Header("Movement")]
		[SerializeField, Min(0f)] private float _normalSpeed = MovementRules.BotSpeed;
		[SerializeField, Range(0f, 1f)] private float _massSpeedPenalty = 0f;
		[SerializeField, Range(0.1f, 1f)]
		private float _minimumSpeedMultiplier = 1f;

		[Header("Turning")]
		[SerializeField, Min(0f), FormerlySerializedAs("_turnSpeed")]
		private float _smoothTurnSpeed = 260f;
		[SerializeField, Min(0f)] private float _dashTurnSpeed = 900f;
		[SerializeField, Range(0f, 1f)] private float _dashChance = 0.45f;
		[SerializeField, Min(0.05f)] private float _dashDuration = 0.3f;
		[SerializeField, Min(0.1f)] private float _minimumDashInterval = 3f;
		[SerializeField, Min(0.1f)] private float _maximumDashInterval = 9f;

		[Header("Awareness")]
		[SerializeField, Min(1f)] private float _awarenessRadius = 22f;
		[SerializeField, Min(MinimumThinkInterval)]
		private float _thinkInterval = MinimumThinkInterval;
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

		private float _baseAwarenessRadius;
		private float _baseHuntSizeRatio;
		private float _baseThreatSizeRatio;
		private float _baseNormalSpeed;

		private readonly Collider2D[] _nearbyColliders =
			new Collider2D[NearbyColliderCapacity];
		private ContactFilter2D _nonFoodFilter;
		private IGameStateService _gameStateService;
		private IFoodFieldGuide _fieldGuide;
		private IPlayerRegistry _playerRegistry;
		private Vector2 _desiredDirection = Vector2.right;
		private Vector2 _separationForce;
		private float _stateTimeRemaining;
		private float _thinkTimeRemaining;
		private bool _dashEnabled;
		private float _dashTimeRemaining;
		private float _nextDashDelay;

		private IFoodService _foodSpawner;
		private BotDifficultyTuning _difficultyTuning;
		private PlanetBody _targetPlanet;
		private Transform _boostDropPoint;
		private float _boostRemaining;
		private float _boostCooldown;
		private float _boostTick;
		public BotDifficulty Difficulty { get; private set; } = BotDifficulty.Medium;
		public bool IsBoosting => _enemy != null && _enemy.IsBoosting;

		private void ApplyDifficulty()
		{
			Difficulty = _enemy.DifficultyLevel;
			_difficultyTuning = BotDifficultyRules.For(Difficulty);
			BotTuning personality = BotTuning.For(Personality);
			_awarenessRadius = _baseAwarenessRadius * personality.Awareness * _difficultyTuning.Awareness;
			_huntSizeRatio = Mathf.Max(
				CombatRules.DefaultEatSizeRatio,
				_baseHuntSizeRatio * personality.HuntRatio * _difficultyTuning.HuntRatio / BotDifficultyRules.MediumHuntRatio
			);
			_threatSizeRatio = Mathf.Min(
				CombatRules.DefaultEatSizeRatio,
				_baseThreatSizeRatio * personality.ThreatRatio
			);
			_thinkInterval = _difficultyTuning.ReactionSeconds;
		}

		public Vector2 Direction { get; private set; } = Vector2.right;
		public MovementState State { get; private set; } =
			MovementState.Inactive;

		private void Awake()
		{
			_enemyTransform ??= transform;
			_rigidbody2D ??= GetComponent<Rigidbody2D>();
			_enemy ??= GetComponent<Enemy>();
			_baseAwarenessRadius = _awarenessRadius;
			_baseHuntSizeRatio = _huntSizeRatio;
			_baseThreatSizeRatio = _threatSizeRatio;
			_baseNormalSpeed = _normalSpeed;

			int foodMask = LayerMask.GetMask(Constants.FoodLayerName);
			_nonFoodFilter = ContactFilter2D.noFilter;
			_nonFoodFilter.SetLayerMask(~foodMask);
		}

		public BotPersonality Personality { get; private set; }

		private void ApplyPersonality(BotPersonality personality)
		{
			Personality = personality;
			_normalSpeed = _baseNormalSpeed * BotTuning.For(personality).Speed;
		}

		[Inject]
		public void Construct(
			IGameStateService gameStateService,
			IFoodFieldGuide fieldGuide,
			IPlayerRegistry playerRegistry,
			IFoodService foodSpawner
		)
		{
			_gameStateService = gameStateService ?? throw new ArgumentNullException(nameof(gameStateService));
			_fieldGuide = fieldGuide ?? throw new ArgumentNullException(nameof(fieldGuide));
			_playerRegistry = playerRegistry ?? throw new ArgumentNullException(nameof(playerRegistry));
			_foodSpawner = foodSpawner;
		}

		public override void OnNetworkSpawn()
		{
			base.OnNetworkSpawn();

			if (IsServer)
			{
				ApplyPersonality(BotTuning.PersonalityFor(NetworkObjectId));
				ApplyDifficulty();
				_thinkTimeRemaining = Random.Range(0f, _thinkInterval);
				InitializeDashing();
				EnterRoaming();
			}
		}

		public override void OnNetworkDespawn()
		{
			StopMovement();
			StopBoost();
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
				StopBoost();
				return;
			}

			float deltaTime = Time.fixedDeltaTime;
			UpdateMoonRaid(deltaTime);
			_stateTimeRemaining -= deltaTime;
			_thinkTimeRemaining -= deltaTime;

			if (_thinkTimeRemaining <= 0f)
			{
				if (_playerRegistry.IsAnyPlayerWithinDistance(_enemyTransform.position, IdleCullDistance))
				{
					Think();
				}
				else
				{
					_separationForce = Vector2.zero;
					EnsureRoaming();
				}

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

			UpdateBoost(deltaTime);
			float turnSpeed = UpdateTurnSpeed(deltaTime);
			UpdateDirection(deltaTime, turnSpeed);
			Move();
			RotateTowardsDirection(deltaTime, turnSpeed);
		}

		public void Move()
		{
			float speedMultiplier =
				EnemyDecisionRules.GetCapacitySpeedMultiplier(
					_enemy.Capacity,
					_enemy.MinCapacity,
					_massSpeedPenalty,
					_minimumSpeedMultiplier
				);

			float boostMultiplier = IsBoosting ? BotDifficultyRules.BoostSpeedMultiplier : 1f;
			if (_raidTarget != null)
			{
				Direction = MoonRaidRules.AvoidCore(
					_rigidbody2D.position,
					_raidTarget.transform.position,
					_enemy.BodyRadius * 1.1f + _raidTarget.BodyRadius,
					Direction,
					_normalSpeed * speedMultiplier * boostMultiplier * Time.fixedDeltaTime
				);
			}
			_rigidbody2D.linearVelocity = WorldBounds.KeepInside(
				_rigidbody2D.position,
				Direction * (_normalSpeed * speedMultiplier * boostMultiplier)
			);
		}

		private void StopBoost()
		{
			_boostRemaining = 0f;
			_boostTick = 0f;
			_enemy?.SetBoosting(false);
		}

		private void UpdateBoost(float deltaTime)
		{
			_boostCooldown = Mathf.Max(0f, _boostCooldown - deltaTime);
			bool evading = State == MovementState.Evading;
			bool hunting = State == MovementState.Hunting &&
				_targetPlanet != null &&
				_targetPlanet.IsSpawned &&
				!_targetPlanet.IsHiddenFromRivals &&
				_enemy.CanDevour(_targetPlanet);
			float distance = hunting ? Vector2.Distance(transform.position, _targetPlanet.transform.position) : 0f;
			bool wanted = BotDifficultyRules.ShouldBoost(
				Difficulty,
				evading,
				hunting,
				_enemy.Capacity,
				Mathf.Max(_enemy.InitialCapacity, _enemy.MinCapacity),
				hunting ? _targetPlanet.Capacity : 0f,
				distance,
				Vector2.Dot(Direction, _desiredDirection)
			);
			if (_boostRemaining <= 0f && _boostCooldown <= 0f && wanted)
			{
				_boostRemaining = _difficultyTuning.BoostSeconds;
				_boostCooldown = _boostRemaining + _difficultyTuning.BoostCooldown;
				_boostTick = 0f;
			}
			if (!wanted || _boostRemaining <= 0f)
			{
				StopBoost();
				return;
			}
			_boostRemaining = Mathf.Max(0f, _boostRemaining - deltaTime);
			_boostTick -= deltaTime;
			if (_boostTick <= 0f)
			{
				if (!_enemy.TrySpendBoostMass(out float spent))
				{
					StopBoost();
					return;
				}
				_boostTick += BotDifficultyRules.BoostTickSeconds;
				DropBoostFood(spent);
			}
			_enemy.SetBoosting(_boostRemaining > 0f);
		}

		private void DropBoostFood(float spent)
		{
			if (_foodSpawner == null)
			{
				return;
			}
			if (_boostDropPoint == null)
			{
				_boostDropPoint = new GameObject("BoostDropPoint").transform;
				_boostDropPoint.SetParent(transform, false);
			}
			_enemy.DropBoostFood(
				_foodSpawner,
				_boostDropPoint,
				Direction,
				spent * BotDifficultyRules.BoostFoodFraction
			);
		}

		public void ResetForRespawn()
		{
			if (!IsServer)
			{
				return;
			}

			StopMovement();
			StopBoost();
			_boostCooldown = 0f;
			_targetPlanet = null;
			ResetMoonRaid();
			ApplyDifficulty();
			_separationForce = Vector2.zero;
			Direction = Constants.GetRandomDirection();
			_desiredDirection = Direction;
			_thinkTimeRemaining = Random.Range(0f, _thinkInterval);
			InitializeDashing();
			EnterRoaming();
		}

		public void EvadeFrom(Vector2 threatPosition)
		{
			if (!IsSpawned || !IsServer)
			{
				return;
			}

			_desiredDirection = Constants.NormalizedOrRandom((Vector2)_enemyTransform.position - threatPosition);

			State = MovementState.Evading;
			_stateTimeRemaining = _evasionDuration;

			if (_dashEnabled)
			{
				StartDash();
			}
		}

		private void InitializeDashing()
		{
			_dashEnabled = Random.value < _dashChance;
			_dashTimeRemaining = 0f;
			_nextDashDelay = NextDashDelay();
		}

		private float NextDashDelay() => Random.Range(
			_minimumDashInterval,
			Mathf.Max(_minimumDashInterval, _maximumDashInterval)
		);

		private void StartDash()
		{
			_dashTimeRemaining = _dashDuration;
			_nextDashDelay = NextDashDelay();
		}

		private float UpdateTurnSpeed(float deltaTime)
		{
			if (_dashEnabled)
			{
				if (_dashTimeRemaining > 0f)
				{
					_dashTimeRemaining = Mathf.Max(0f, _dashTimeRemaining - deltaTime);
				}
				else
				{
					_nextDashDelay -= deltaTime;
					if (_nextDashDelay <= 0f)
					{
						StartDash();
					}
				}
			}

			return EnemyDecisionRules.GetTurnSpeed(_smoothTurnSpeed, _dashTurnSpeed, _dashTimeRemaining);
		}

		private void Think()
		{
			ApplyDifficulty();
			Vector2 position = _enemyTransform.position;
			(PlanetBody targetPlanet, Vector2 hazardPosition, float hazardDistanceSquared, Vector2 separationAccum) =
				ScanSurroundings(position);
			FoodRecord nearestFood = FindNearestFood(position);

			_separationForce = separationAccum;

			bool hasImmediateHazard = hazardDistanceSquared <= _hazardDistance * _hazardDistance;
			_targetPlanet = targetPlanet;
			if (TryMoonRaid(targetPlanet, hasImmediateHazard))
			{
				return;
			}
			EnemyIntent intent = EnemyDecisionRules.ChooseIntent(
				_enemy.Capacity,
				targetPlanet?.Capacity ?? 0f,
				nearestFood != null,
				hasImmediateHazard,
				_huntSizeRatio,
				_threatSizeRatio
			);

			switch (intent)
			{
				case EnemyIntent.Evade:
					Vector2 dangerPosition = hasImmediateHazard
						? hazardPosition
						: targetPlanet.transform.position;
					EvadeFrom(dangerPosition);
					break;

				case EnemyIntent.Hunt:
					Vector2 velocity = targetPlanet.TryGetComponent(out Rigidbody2D targetBody)
						? targetBody.linearVelocity
						: Vector2.zero;
					SetTarget(
						WorldBounds.Clamp(
							BotDifficultyRules.PursuitPoint(
								targetPlanet.transform.position,
								velocity,
								_difficultyTuning.PredictionSeconds
							)
						),
						MovementState.Hunting
					);
					break;

				case EnemyIntent.Forage:
					SetTarget(nearestFood.Position, MovementState.Foraging);
					break;

				default:
					EnsureRoaming();
					break;
			}
		}

		private (PlanetBody TargetPlanet, Vector2 HazardPosition, float HazardDistanceSquared, Vector2 SeparationForce
			)
			ScanSurroundings(Vector2 position)
		{
			int hitCount = Physics2D.OverlapCircle(position, _awarenessRadius, _nonFoodFilter, _nearbyColliders);

			PlanetBody targetPlanet = null;
			Vector2 hazardPosition = default;
			Vector2 separationAccum = Vector2.zero;
			float targetDistanceSquared = float.PositiveInfinity;
			float hazardDistanceSquared = float.PositiveInfinity;
			float separationRadiusSquared = (_enemy.Capacity * SeparationRadiusPerUnit) *
				(_enemy.Capacity * SeparationRadiusPerUnit);

			for (int index = 0; index < hitCount; index++)
			{
				Collider2D candidate = _nearbyColliders[index];
				if (candidate == null || candidate.transform == _enemyTransform)
				{
					continue;
				}

				Vector2 candidatePosition = candidate.transform.position;
				float distanceSquared = (candidatePosition - position).sqrMagnitude;

				if (candidate.TryGetComponent(out PlanetBody otherPlanet))
				{
					if (otherPlanet.IsHiddenFromRivals ||
						otherPlanet is Player { IsDefeated: true } ||
						otherPlanet is Player { IsSpawnProtected: true })
					{
						continue;
					}
					if (otherPlanet.IsMoonThreat(position, _enemy.BodyRadius, out Vector2 moonPosition))
					{
						hazardPosition = moonPosition;
						hazardDistanceSquared = 0f;
					}
					if (otherPlanet is Enemy && distanceSquared < separationRadiusSquared && distanceSquared > 0.001f)
					{
						Vector2 push = position - candidatePosition;
						float distance = Mathf.Sqrt(distanceSquared);
						separationAccum += push / (distance * Mathf.Max(distance, 0.3f));
					}
					bool candidateIsThreat = otherPlanet.CanDevour(_enemy);
					bool targetIsThreat = targetPlanet != null && targetPlanet.CanDevour(_enemy);
					bool preferred = candidateIsThreat != targetIsThreat
						? candidateIsThreat
						: distanceSquared < targetDistanceSquared;
					if (targetPlanet == null || preferred)
					{
						targetPlanet = otherPlanet;
						targetDistanceSquared = distanceSquared;
					}
					continue;
				}

				if (candidate.TryGetComponent(out Comet _) &&
					distanceSquared < hazardDistanceSquared)
				{
					hazardPosition = candidatePosition;
					hazardDistanceSquared = distanceSquared;
				}
			}

			return (targetPlanet, hazardPosition, hazardDistanceSquared, separationAccum);
		}

		private readonly System.Collections.Generic.List<FoodRecord> _foodRecords = new(128);
		private FoodRecord FindNearestFood(Vector2 position)
		{
			if (_foodSpawner == null) return null;
			_foodSpawner.Query(position, Mathf.Min(_awarenessRadius, FoodSearchRadius), _foodRecords);
			FoodRecord nearestFood = null;
			float score = float.PositiveInfinity;
			foreach (FoodRecord food in _foodRecords)
			{
				if (food.IsMagnetImmuneFor(NetworkObjectId)) continue;
				float candidate = (food.Position - position).sqrMagnitude / Mathf.Max(0.0001f, food.GetGrowth(1f));
				if (candidate < score) { score = candidate; nearestFood = food; }
			}
			return nearestFood;
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

		private void EnsureRoaming()
		{
			if (State != MovementState.Roaming || _stateTimeRemaining <= 0f)
			{
				EnterRoaming();
			}
		}

		private void EnterRoaming()
		{
			State = MovementState.Roaming;
			_desiredDirection = GetRoamDirection();

			float min = Mathf.Max(MinimumRoamTime, _minimumTimeToChangeDirection);
			float max = Mathf.Max(min, _maximumTimeToChangeDirection);
			_stateTimeRemaining = Random.Range(min, max);
		}

		private Vector2 GetRoamDirection()
		{
			if (_fieldGuide != null &&
				Random.value < FieldSeekChance &&
				_fieldGuide.TryGetFieldTarget(_enemyTransform.position, FieldSeekMinimumDistance, out Vector2 target))
			{
				Vector2 toTarget = target - (Vector2)_enemyTransform.position;
				if (toTarget.sqrMagnitude > Constants.MinimumDirectionSquaredMagnitude)
				{
					return toTarget.normalized;
				}
			}

			return Constants.GetRandomDirection();
		}

		private void UpdateDirection(float deltaTime, float turnSpeed)
		{
			Vector2 border = BotDifficultyRules.BorderAvoidance(
				_enemyTransform.position,
				WorldBounds.Area,
				_enemy.BodyRadius + 3f
			);
			Vector2 target = border != Vector2.zero ? border : _desiredDirection;

			if (_separationForce.sqrMagnitude > 0.0001f)
			{
				target += _separationForce * SeparationWeight;
			}

			if (target.sqrMagnitude <= Constants.MinimumDirectionSquaredMagnitude)
			{
				return;
			}

			target.Normalize();

			Direction = Vector3.RotateTowards(
				Direction,
				target,
				turnSpeed * Mathf.Deg2Rad * deltaTime,
				0f
			).normalized;
		}

		private void RotateTowardsDirection(float deltaTime, float turnSpeed)
		{
			if (Direction.sqrMagnitude <= Constants.MinimumDirectionSquaredMagnitude)
			{
				return;
			}

			float targetAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
			_rigidbody2D.MoveRotation(
				Mathf.MoveTowardsAngle(_rigidbody2D.rotation, targetAngle, turnSpeed * deltaTime)
			);
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
			_maximumTimeToChangeDirection = Mathf.Max(_minimumTimeToChangeDirection, _maximumTimeToChangeDirection);
			_maximumDashInterval = Mathf.Max(_minimumDashInterval, _maximumDashInterval);
			_awarenessRadius = Mathf.Max(_hazardDistance, _awarenessRadius);
		}
		#endif
	}
}