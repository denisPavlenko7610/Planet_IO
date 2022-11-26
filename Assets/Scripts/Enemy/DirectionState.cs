using RDTools.AutoAttach;
using UnityEngine;


namespace Planet_IO
{
    [RequireComponent(typeof(EnemyMovement))]
    public class DirectionState : EnemyState
    {
        [SerializeField, Attach] private EnemyMovement enemyMovement;

        private bool _changeDirection;

        public override  EnemyState RunCurrentState()
        {
            if (_changeDirection)
            {
                _changeDirection = false;
                return enemyMovement;
            }

            ChangeDirection();
            return this;
        }

        private void ChangeDirection()
        {
            enemyMovement.Direction = enemyMovement.DirectionMove(enemyMovement.Direction);
            _changeDirection = true;
        }

        public void Evade(Vector2 direction)
        {
            Vector2 tepmVector = Vector2.Reflect(enemyMovement.Direction.normalized, direction.normalized);
            enemyMovement.Direction = -tepmVector;
        }
    }
}