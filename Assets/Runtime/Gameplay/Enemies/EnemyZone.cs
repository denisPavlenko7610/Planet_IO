using UnityEngine;

namespace Planet_IO
{
    public class EnemyZone : MonoBehaviour
    {
        [SerializeField] private DirectionState directionState;
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.TryGetComponent(out BordersTrigger _bordersTrigger))
            {
                directionState.Evade(_bordersTrigger.transform.position);
            }

            if (col.TryGetComponent(out Comet _comet))
            {
                directionState.Evade(_comet.transform.position);
            }
        }
    }
}
