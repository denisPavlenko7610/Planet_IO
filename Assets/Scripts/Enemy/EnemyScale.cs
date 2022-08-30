using UnityEngine;
using UnityEngine.Events;

namespace Planet_IO
{
    public class EnemyScale : MonoBehaviour, ICapacity
    {
        private const float _zero = 0f;
        
        public float Capacity { get; set; }
        [SerializeField] private float _minCapacityEnemy;
        [SerializeField] private float _maxCapacityEnemy;

        private UnityEvent<float> _changeCapacity = new();

        private void Awake() => Capacity = transform.localScale.x;

        private void Start() => _changeCapacity.AddListener(DeathCheck);

        public void SetEnemyCapacity(float scaleValue)
        {
            if (Capacity < _maxCapacityEnemy) IncreaseScale(scaleValue);
        }
        
        private void DeathCheck(float capacity)
        {
            if (capacity < _minCapacityEnemy) print("Die");
        }
        
        private void IncreaseScale(float scaleValue)
        {
            var scaleDivider = 100;
            scaleValue /= scaleDivider;
            IncreaseEnemyCapacity(scaleValue);
            _changeCapacity.Invoke(Capacity);
            transform.localScale += new Vector3(scaleValue, scaleValue, _zero);
        }

        private void IncreaseEnemyCapacity(float scaleValue) => Capacity += scaleValue;
    }
}
