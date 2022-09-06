using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class CometMovement : MonoBehaviour, IMove
    {
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private Transform _cometTransform;
        [SerializeField] private float _minSpeed = 0.01f;
        [SerializeField] private float _maxSpeed = 0.03f;

        public float NormalSpeed { get; private set; } = 0.004f;
        public float BoostSpeed { get; private set; }
        
        private Vector2 _direction;

        private void Start()
        {
            NormalSpeed = RandomSpeed();
            _direction = DirectionMove(_direction);
            Move();
        }

        private void Update() => Rotation();

        private Vector2 DirectionMove(Vector2 Direction)
        {
            var rangeValue = 1f;
            Direction.x = Random.Range(-rangeValue, rangeValue);
            Direction.y = Random.Range(-rangeValue, rangeValue);
            return Direction;
        }
        private float RandomSpeed() => Random.Range(_minSpeed, _maxSpeed);

        public void Move() => _rigidbody2D.AddForce(NormalSpeed * _direction);

        private void Rotation() => _cometTransform.Rotate(0, 0, _direction.y );
    }
}
