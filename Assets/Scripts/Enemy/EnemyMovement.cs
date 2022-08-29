using UnityEngine;
using Random = UnityEngine.Random;


namespace Planet_IO
{
    public class EnemyMovement : MonoBehaviour, IMove
    {
        [SerializeField] private float _speed;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        private Vector2 _direction;


        private void Start()
        {
            _direction = DirecationMove(_direction);
        }

        private void FixedUpdate()
        {
            Move();
        }

        private Vector2 DirecationMove(Vector2 Direction)
        {
            var rangeValue = 1f;
            Direction.x = Random.Range(-rangeValue, rangeValue);
            Direction.y = Random.Range(-rangeValue, rangeValue);
            return Direction;
        }

        public void Move()
        {
            _rigidbody2D.velocity = _direction * _speed;
        }
    }
}
