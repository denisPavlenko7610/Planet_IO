using System;
using Cysharp.Threading.Tasks;
using Dythervin.AutoAttach;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    [RequireComponent(typeof(EnemyMovement))]
    public class EnemyChangeOfDirection : MonoBehaviour
    {
        [SerializeField, Attach] private EnemyMovement _enemyMovement;
        
        [Header("Time")]
        [SerializeField] private float _maxTimeToChangeDirection;
        [SerializeField] private float _minTimeToChangeDirection;
        private float _timeToChangeDirection;

        private void Start() => ChangeDirection();

        public async void ChangeDirection()
        {
            while (true)
            {
                _enemyMovement.Direction = _enemyMovement.DirectionMove(_enemyMovement.Direction);
                _timeToChangeDirection = TimeChangeDirection();
                await UniTask.Delay(TimeSpan.FromSeconds(_timeToChangeDirection));
            }
        }
        private float TimeChangeDirection() => _timeToChangeDirection = Random.Range(_minTimeToChangeDirection, _maxTimeToChangeDirection);
    }
}
