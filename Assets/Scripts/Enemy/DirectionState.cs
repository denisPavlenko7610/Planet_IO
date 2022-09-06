using Dythervin.AutoAttach;
using UnityEngine;


namespace Planet_IO
{
    [RequireComponent(typeof(Movement))]
    public class DirectionState : MonoBehaviour, IState
    {
        [SerializeField, Attach] private Movement _movement;

        private bool _changeDirection;

        public IState RunCurrentState()
        {
            if (_changeDirection)
            {
                _changeDirection = false;
                return _movement;
            }

            ChangeDirection();
            return this;
        }

        private void ChangeDirection()
        {
            _movement.Direction = _movement.DirectionMove(_movement.Direction);
            _changeDirection = true;
        }

        public void Evade(Vector2 direction)
        {
            Vector2 tepmVector = Vector2.Reflect(_movement.Direction.normalized, direction.normalized);
            _movement.Direction = -tepmVector;
        }
    }
}