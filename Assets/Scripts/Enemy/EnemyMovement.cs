using Dythervin.AutoAttach;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Planet_IO
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyMovement : MonoBehaviour, IMove
    {
        public Vector2 Direction { get; set; }
        [field: SerializeField] public float NormalSpeed { get;  set; }
        public float BoostSpeed { get; set; }
        [SerializeField, Attach] private Rigidbody2D _rigidbody2D;
        
        

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
