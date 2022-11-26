using System;
using UnityEngine;

namespace Planet_IO
{
    public abstract class PlanetScale : MonoBehaviour, ICapacity
    {
        [field: Header("Capacity Player")]
        [field: SerializeField]
        public float MinCapacity { get; private set; } = 0.08f;

        [SerializeField] protected float _maxCapacity = 10f;
        public float Capacity { get; set; }
        protected event Action<float> _changeCapacity;

        protected void Init()
        {
            Capacity = transform.localScale.x;
            _changeCapacity += DeathCheck;
        }

        protected virtual void DecreaseCapacity(float scaleValue)
        {
            var scaleDivider = 2f;
            scaleValue /= scaleDivider;
            if (scaleValue <= MinCapacity)
                scaleValue = MinCapacity;

            Capacity = scaleValue;
            transform.localScale = new(scaleValue, scaleValue, 0);
        }

        protected virtual void SetCapacity(float scaleValue)
        {
            if (Capacity < _maxCapacity)
            {
                IncreaseScale(scaleValue);
            }
        }

        protected virtual void IncreaseScale(float scaleValue)
        {
            var scaleDivider = 100;
            scaleValue /= scaleDivider;
            IncreaseCapacity(scaleValue);
            _changeCapacity?.Invoke(Capacity);
            transform.localScale += new Vector3(scaleValue, scaleValue, 0);
        }

        protected virtual void DeathCheck(float capacity)
        {
            if (capacity < MinCapacity)
                print("Die");
        }

        protected virtual void IncreaseCapacity(float scaleValue) => Capacity += scaleValue;
    }
}