using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Planet_IO
{
    public class EnemyMovement : MonoBehaviour, IMove
    {
        [SerializeField] private float _speed;
        [SerializeField] private Rigidbody2D _rigidbody2D;
        [SerializeField] private float _timeToChangeDirection;
        private Vector2 _direction;


        private void Start() => ChangeOfDirection();

        private void FixedUpdate() => Move();

        private Vector2 DirectionMove(Vector2 Direction)
        {
            var rangeValue = 1f;
            Direction.x = Random.Range(-rangeValue, rangeValue);
            Direction.y = Random.Range(-rangeValue, rangeValue);
            return Direction;
        }

        private async void ChangeOfDirection()
        {
            while (true)
            {
                _direction = DirectionMove(_direction);
                await UniTask.Delay(TimeSpan.FromSeconds(_timeToChangeDirection));
            }
        }

        public void Move()
        {
            _rigidbody2D.velocity = _direction * _speed;
        }
    }
}
