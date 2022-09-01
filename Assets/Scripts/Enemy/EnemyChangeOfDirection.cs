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
            ChangeDirection();
            return this;
        }
        private  void ChangeDirection()
        {
            _enemyMovement.Direction = _enemyMovement.DirectionMove(_enemyMovement.Direction);
            _changeDirection = true;
        }

        public void Evade(Vector2 direction)
        {
            Vector2 Tepm = Vector2.Reflect(_enemyMovement.Direction.normalized,direction.normalized);
            _enemyMovement.Direction = -Tepm;
        }
    }
}
