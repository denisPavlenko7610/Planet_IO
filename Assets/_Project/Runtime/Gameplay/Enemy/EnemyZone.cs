using UnityEngine;

namespace PlanetIO
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyMovement))]
    public sealed class EnemyZone : MonoBehaviour
    {
        [SerializeField] private EnemyMovement _enemyMovement;

        private void Awake()
        {
            _enemyMovement ??= GetComponent<EnemyMovement>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_enemyMovement == null || other == null)
            {
                return;
            }

            if (other.TryGetComponent(out BordersTrigger border))
            {
                _enemyMovement.EvadeFrom(border.transform.position);
            }
            else if (other.TryGetComponent(out Comet comet))
            {
                _enemyMovement.EvadeFrom(comet.transform.position);
            }
        }
    }
}
