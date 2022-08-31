using Dythervin.AutoAttach;
using UnityEngine;


namespace Planet_IO
{
    [RequireComponent(typeof(EnemyMovement))]
    public class EnemyChangeOfDirection : EnemyState
    {
        [SerializeField, Attach] private EnemyMovement _enemyMovement;
        private bool _changeDirection;
        public override EnemyState RunCurrentState()
        {
            if (_changeDirection)
            {
                _changeDirection = false;
                return _enemyMovement;
            }
            else
            {
                ChangeDirection();
                return this;
            }
            
        }
        private  void ChangeDirection()
        {
            _enemyMovement.Direction = _enemyMovement.DirectionMove(_enemyMovement.Direction);
            _changeDirection = true;
        }
    }
}
