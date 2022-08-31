using UnityEngine;

namespace Planet_IO
{
    public class EnemyZone : MonoBehaviour
    {
        [SerializeField] private EnemyChangeOfDirection _enemyChangeOfDirection;
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out BordersTrigger _bordersTrigger))
            {
                _enemyChangeOfDirection.Evade(_bordersTrigger.transform.position);
            }

            if (col.TryGetComponent(out Comet _comet))
            {
                _enemyChangeOfDirection.Evade(_comet.transform.position);
            }
        }
    }
}
