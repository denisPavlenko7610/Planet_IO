using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    public class Movement : EnemyState, IMove
    {
        public DirectionState DirectionState;

        [Header("Time")] [SerializeField] private float _maxTimeToChangeDirection;
        [SerializeField] private float _minTimeToChangeDirection;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        
        [field: SerializeField] public float NormalSpeed { get; private set; }

        private float _timeToChangeDirection;

        public float BoostSpeed { get; set; }
        public Vector2 Direction { get; set; }


        public override EnemyState RunCurrentState()
        {
            if (_timeToChangeDirection < 0)
            {
                _timeToChangeDirection = TimeChangeDirection();
                return DirectionState;
            }

            return this;
        }

        private void FixedUpdate()
        {
            _timeToChangeDirection -= Time.fixedDeltaTime;
            Move();
        }

        private float TimeChangeDirection() => _timeToChangeDirection =
            Random.Range(_minTimeToChangeDirection, _maxTimeToChangeDirection);

        public void Move() => _rigidbody2D.velocity = Direction * NormalSpeed;

        public Vector2 DirectionMove(Vector2 Direction)
        {
            var rangeValue = 1f;
            Direction.x = Random.Range(-rangeValue, rangeValue);
            Direction.y = Random.Range(-rangeValue, rangeValue);
            return Direction;
        }
    }
}