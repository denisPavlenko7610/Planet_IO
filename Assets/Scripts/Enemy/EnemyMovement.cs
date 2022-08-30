using System;
using Cysharp.Threading.Tasks;
using Dythervin.AutoAttach;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    public class EnemyMovement : MonoBehaviour, IMove
    {
        [field: SerializeField] public float NormalSpeed { get;  set; }
        public float BoostSpeed { get; set; }
        [SerializeField, Attach] private Rigidbody2D _rigidbody2D;
        
        [Header("Time")]
        [SerializeField] private float _maxTimeToChangeDirection;
        [SerializeField] private float _minTimeToChangeDirection;
        private float _timeToChangeDirection;
        
        private Vector2 _direction;

        

        private void Start() => ChangeOfDirection();

        private void FixedUpdate() => Move();

        public void Move() => _rigidbody2D.velocity = _direction * NormalSpeed;
        private Vector2 DirectionMove(Vector2 Direction)
        {
            var rangeValue = 1f;
            Direction.x = Random.Range(-rangeValue, rangeValue);
            Direction.y = Random.Range(-rangeValue, rangeValue);
            return Direction;
        }

        private float TimeChangeDirection() => _timeToChangeDirection = Random.Range(_minTimeToChangeDirection, _maxTimeToChangeDirection);

        private async void ChangeOfDirection()
        {
            while (true)
            {
                _direction = DirectionMove(_direction);
                _timeToChangeDirection = TimeChangeDirection();
                await UniTask.Delay(TimeSpan.FromSeconds(_timeToChangeDirection));
            }
        }
    }
}
