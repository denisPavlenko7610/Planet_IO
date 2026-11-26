using System;
using VContainer;
using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlanetGravity : MonoBehaviour
    {
        private readonly SlingshotState _slingshot = new();

        private MatchMigrationStore _migration;
        [Inject] public void ConfigureMigration(MatchMigrationStore store) => _migration = store;
        private PlanetBody _planet;
        private Rigidbody2D _rigidbody2D;

        public float SlingshotBoost => _slingshot.Boost;

        public event Action SlingshotTriggered;

        private void Awake()
        {
            _planet = GetComponent<PlanetBody>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnDisable()
        {
            _slingshot.Reset();
        }

		private void FixedUpdate()
		{
			if (_planet == null ||
				_rigidbody2D == null ||
				!_planet.IsSpawned ||
				!_planet.IsGravitySimulatedHere ||
				!_planet.AreMoonsVisible)
			{
				_slingshot.Reset();
				return;
			}

			if (_migration?.IsRecovering == true)
			{
				return;
			}

			float deltaTime = Time.fixedDeltaTime;
			Vector2 position = _rigidbody2D.position;
			PullSum pull = default;
			AddWellPull(position, ref pull);
			AddAnchorPull(position, ref pull);

			if (pull.StrongestSpeed > 0f)
			{
				_rigidbody2D.position = Constants.ClampToWorld(position + pull.Total * deltaTime);
			}

			bool tangential = pull.StrongestSpeed > 0f &&
				GravityRules.IsTangential(_rigidbody2D.linearVelocity, pull.StrongestDirection);
			if (_slingshot.Tick(deltaTime, tangential))
			{
				SlingshotTriggered?.Invoke();
			}
		}

		private void AddWellPull(Vector2 position, ref PullSum pull)
		{
			var planets = PlanetDirectory.All;
			for (int index = 0; index < planets.Count; index++)
			{
				PlanetBody well = planets[index];
				if (well == null ||
					well == _planet ||
					!well.isActiveAndEnabled ||
					!well.IsSpawned ||
					!well.AreMoonsVisible ||
					!well.Pulls(_planet))
				{
					continue;
				}

				Vector2 toward = (Vector2)well.transform.position - position;
				float zoneRadius = well.GravityZoneRadius;
				float squaredDistance = toward.sqrMagnitude;
				if (squaredDistance >= zoneRadius * zoneRadius)
				{
					continue;
				}

				float distance = Mathf.Sqrt(squaredDistance);
				if (distance < 0.01f)
				{
					continue;
				}

				float speed = GravityRules.PullSpeed(distance, well.BodyRadius, zoneRadius) *
					MapFeatureField.GravityScale;
				if (speed > 0f)
				{
					pull.Add(toward, distance, speed);
				}
			}
		}

		private static void AddAnchorPull(Vector2 position, ref PullSum pull)
		{
			MapFeatureField.Anchor[] anchors = MapFeatureField.Anchors;
			for (int index = 0; index < anchors.Length; index++)
			{
				MapFeatureField.Anchor anchor = anchors[index];
				if (anchor.Strength <= 0f)
				{
					continue;
				}

				Vector2 toward = anchor.Position - position;
				float distance = toward.magnitude;
				if (distance < 0.01f)
				{
					continue;
				}

				float speed = GravityRules.PullSpeed(distance, anchor.Radius, anchor.ZoneRadius) *
					anchor.Strength * MapFeatureField.GravityScale;
				if (speed > 0f)
				{
					pull.Add(toward, distance, speed);
				}
			}
		}

		private struct PullSum
		{
			public Vector2 Total;
			public Vector2 StrongestDirection;
			public float StrongestSpeed;

			public void Add(Vector2 toward, float distance, float speed)
			{
				Total += toward / distance * speed;
				if (speed > StrongestSpeed)
				{
					StrongestSpeed = speed;
					StrongestDirection = toward;
				}
			}
		}
    }
}
