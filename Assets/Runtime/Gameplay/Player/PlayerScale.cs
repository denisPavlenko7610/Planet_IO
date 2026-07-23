using UnityEngine;

namespace Planet_IO
{
    public class PlayerScale : PlanetScale
    {
        public bool IsDie { get; private set; }

        protected Rigidbody2D _rigidbody2D;

        protected override void Awake()
        {
            base.Awake();
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        protected override void DeathCheck(float capacity)
        {
            if (capacity <= MinCapacity)
            {
                IsDie = true;
                if (_rigidbody2D != null)
                {
                    _rigidbody2D.linearVelocity = Vector2.zero;
                }
            }
        }
    }
}
