using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    public class EnemyMovement : EnemyState, IMove
    {
        public EnemyChangeOfDirection _enemyChangeOfDirection;
        public Vector2 Direction { get; set; }

        [Header("Time")]
        [SerializeField] private float _maxTimeToChangeDirection;
        [SerializeField] private float _minTimeToChangeDirection;
        private float _timeToChangeDirection;
        
        [field: SerializeField] public float NormalSpeed { get;  set; }
        public float BoostSpeed { get; set; }
        
        [SerializeField] private Rigidbody2D _rigidbody2D;
        
        
        public override EnemyState RunCurrentState()
        {
            if (_timeToChangeDirection < 0)
            {
                _timeToChangeDirection = TimeChangeDirection();
                return _enemyChangeOfDirection;
            }
            return this;
        }

        private void FixedUpdate()
        {
            _timeToChangeDirection -= Time.fixedDeltaTime;
            Move();
        }
        private float TimeChangeDirection() => _timeToChangeDirection = Random.Range(_minTimeToChangeDirection, _maxTimeToChangeDirection);

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
