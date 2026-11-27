using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlanetKnockback : MonoBehaviour
    {
        private const float Deceleration = 16f;
        private const float StopThresholdSquared = 0.0001f;

        private Rigidbody2D _rigidbody2D;
        private Vector2 _velocity;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        private void OnDisable()
        {
            _velocity = Vector2.zero;
        }

        public void Apply(Vector2 velocity)
        {
            _velocity = velocity;
        }

        private void FixedUpdate()
        {
            if (_velocity.sqrMagnitude < StopThresholdSquared || _rigidbody2D == null)
            {
                _velocity = Vector2.zero;
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            Vector2 target = _rigidbody2D.position + _velocity * deltaTime;
			_rigidbody2D.position = WorldBounds.Clamp(target);
            _velocity = Vector2.MoveTowards(_velocity, Vector2.zero, Deceleration * deltaTime);
        }
    }
}
